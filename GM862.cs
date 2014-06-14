/*************************************************************
 * Class to interface a GM862GPS Device
 * The GM862 is a GSM module from Telit with build-in GPRS and GPS
 *
 * (C)opyright 2009 Elze Kool, http://www.microframework.nl
 *
 * This Sourcecode is Public Domain. You are free to use this class Non-Commercialy and Commercialy.
 *
 * This sourcecode is provided AS-IS. I take no responsibility for direct or indirect
 * damage coused by this program/class. 
 *************************************************************/

using System;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace ElzeKool.Devices
{
    /// <summary>
    /// Class to interface a GM862GPS Device
    /// The GM862 is a GSM module from Telit with build-in GPRS and GPS
    ///
    /// (C)opyright 2009 ElzeKool, http://www.microframework.nl
    /// 
    /// This Sourcecode is Public Domain. You are free to use this class Non-Commercialy and Commercialy.
    /// 
    /// This sourcecode is provided AS-IS. I take no responsibility for direct or indirect
    /// damage coused by this program/class. 
    /// </summary>
    public class GM862GPS : IDisposable
    {
        #region Private Internal Variables

        // Serialport used to communicate with GM862
        private SerialPort _ComPort;

        // Variable to check if this object is disposed
       // private bool _Disposed = false;

        // Serial FIFO for Command mode responses
        private String _SerialFIFO_CommandMode;

        // Serial FIFO for Idle mode responses (Unsolicitated Responses)
        private String _SerialFIFI_IdleMode;

        // Current GM862 State
        private GM862GPSStates _CurrentState;
        private Object _CurrentStateLock = new Object();

        // Auto reset event used to signal data reception
        private AutoResetEvent WaitForDataToArrive = new AutoResetEvent(false);

        // Thread that monitors Unsolicitated Responses
        private Thread _ParseUnsolicitatedResponses;

        #endregion

        #region Public Classes

        /// <summary>
        /// States where GM862 can be in
        /// </summary>
        public enum GM862GPSStates
        {
            /// <summary>
            /// GM862 is Idle. Event is triggered when data arrives
            /// </summary>
            Idle,

            /// <summary>
            /// GM862 is in Command mode. An AT command has been send en
            /// data is parsed until a valid response code is returned
            /// </summary>
            Command,

            /// <summary>
            /// GM862 is in Data mode, no commands may be fired except '+++' escape sequence
            /// </summary>
            Data
        }

        /// <summary>
        /// VT25 AT Response Codes
        /// </summary>
        public enum ResponseCodes
        {
            OK,
            CONNECT,
            RING,
            NO_CARRIER,
            ERROR,
            NO_DIALTONE,
            BUSY,
            NO_ANSWER,
            SEND_SMS_DATA
        }

        /// <summary>
        /// Exception used for GM862 related errors
        /// </summary>
        public class GM862GPSException : Exception
        {
            public GM862GPSException(String Message) : base(Message) { }
            public const String FAILED_TO_CREATE = "Failed to create a new SerialPort object";
            public const String NOT_IN_IDLE_STATE = "Not in IDLE State";
            public const String NOT_IN_COMMAND_OR_DATA_STATE = "Can't recieve response in IDLE state";
            public const String TIMEOUT = "Timed-Out while waiting for response";
            public const String MALFORMED_RESPONSE = "Recieved an empty or malformed command response";
        }

        /// <summary>
        /// Class used to return SMS message in it's structure
        /// </summary>
        public class SMSMessage
        {
            public readonly String Memory;
            public readonly int Location;
            public readonly String Status;
            public readonly String Orginator;
            public readonly String ArrivalTime;
            public readonly String Message;

            /// <summary>
            /// Instantiate a new SMS 
            /// </summary>
            public SMSMessage(String Memory, int Location, String Status, String Orginator, String ArrivalTime, String Message)
            {
                this.Memory = Memory;
                this.Location = Location;
                this.Status = Status;
                this.Orginator = Orginator;
                this.ArrivalTime = ArrivalTime;
                this.Message = Message;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Unsolicited response recieved. Allow code to handle response
        /// </summary>
        /// <param name="Response">Recieved response</param>
        public delegate void UnsolicitedResponseEvent(String Response);
        public event UnsolicitedResponseEvent OnUnsolicitedResponse;

        /// <summary>
        /// Event triggered when called
        /// </summary>
        public delegate void RecievingCallEvent();
        public event RecievingCallEvent OnRecievingCall;

        /// <summary>
        /// Event triggered when a SMS is recieved
        /// </summary>
        /// <param name="Storage">Storage for recieved SMS</param>
        /// <param name="Number">Number of recieved SMS</param>
        public delegate void RecievedSMS(String Storage, int Number);
        public event RecievedSMS OnRecievedSMS;

        /// <summary>
        /// Called when there is a PIN request (Like SIM PIN/PUK etc.)
        /// </summary>
        /// <param name="PINType">Type of PIN requested</param>
        /// <returns>Required PIN</returns>
        public delegate string PINRequestHandler(String PINType);
        public PINRequestHandler getRequestedPIN;

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Create new GM862GPS instance
        /// </summary>
        /// <param name="Port">Serial Port to use</param>
        public GM862GPS(String Port)
        {
            // We begin in IDLE state
            _CurrentState = GM862GPSStates.Idle;
            _SerialFIFO_CommandMode = "";
            _SerialFIFI_IdleMode = "";

            try
            {
                // Create new SerialPort Object
                _ComPort = new SerialPort(Port, 19200, Parity.None, 8, StopBits.One);
                _ComPort.Handshake = Handshake.None;
                _ComPort.Open();
            }
            catch
            {
                // If failed throw error
                throw new GM862GPSException(GM862GPSException.FAILED_TO_CREATE);
            }

            // DataRecieved Handler
            _ComPort.DataReceived += new SerialDataReceivedEventHandler(_ComPort_DataReceived);

            // Create thread that handles Unsolicited Responses
            _ParseUnsolicitatedResponses = new Thread(new ThreadStart(ParseUnsolicitedResponse));
            _ParseUnsolicitatedResponses.Priority = ThreadPriority.AboveNormal;
            _ParseUnsolicitatedResponses.Start();

            // Add onUnsolicitedResponse Handler to check for incomming SMS and Call
            OnUnsolicitedResponse += new UnsolicitedResponseEvent(CheckForNewSMS);
            OnUnsolicitedResponse += new UnsolicitedResponseEvent(CheckForCall);
        }

        /// <summary>
        /// Dispose Object
        /// </summary>
        public void Dispose()
        {
            try
            {
            //    _Disposed = true;
                _ComPort.Dispose();
                _ParseUnsolicitatedResponses.Abort();
            }
            catch
            {
            }
        }

        #endregion

        #region Initialization functions

        /// <summary>
        /// Initializes basic GSM Functions and settings
        /// </summary>
        public void InitializeBasicGSM()
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            // Select extended instruction set
            if (ExecuteCommand("AT#SELINT=2", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to select extended instruction set");

            // Reset to factory settings
            if (ExecuteCommand("AT&F1", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to reset to factory settings");

            // Disable local echo
            if (ExecuteCommand("ATE0", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to disable local echo");

            // Disable Flowcontrol
            if (ExecuteCommand("AT&K0", 500) != ResponseCodes.OK)
                throw new GM862GPSException("Failed to disable Flow control");

            // Parse all PIN requests
            while (CheckPIN() == false) { Thread.Sleep(10); }
        }

        /// <summary>
        /// Initialize basic SMS Settings
        /// </summary>
        public void InitializeSMS()
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            // Select standard SMS instruction set
           // if (ExecuteCommand("AT#SMSMODE=0", 500) != GM862GPS.ResponseCodes.OK)
            //    throw new GM862GPSException("Failed to select standard SMS instruction set");

            // Select Unsolicited SMS code to be buffered and in form +CMTI: <mem>, <id>
            if (ExecuteCommand("AT+CNMI=2,1,0,0,0", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to select standard SMS instruction set");

            // Select Text Message Format
            if (ExecuteCommand("AT+CMGF=1", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to select Text SMS format");
        }


        /// <summary>
        /// Initialze GPRS and Activate GPRS Context
        /// </summary>
        /// <param name="ContextID">Context to create/activate</param>
        /// <param name="APN">Access Point</param>
        /// <param name="Username">Username</param>
        /// <param name="Password">Password</param>
        public void ActivateGPRSContext(int ContextID, String APN, String Username, String Password)
        {
            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            // Create new GPRS Context
            if (ExecuteCommand("AT+CGDCONT=" + ContextID.ToString() + ",\"IP\", \"" + APN + "\",\"0.0.0.0\",0,0", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to create new GPRS Context");

            // Set UserID
            if (ExecuteCommand("AT#USERID=\"" + Username + "\"", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to set Username");

            // Set Password
            if (ExecuteCommand("AT#PASSW=\"" + Password + "\"", 500) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to set Password");

            // Check if Context is Active
            String CheckActiveContext;
            if (ExecuteCommand("AT#GPRS?", 500, out CheckActiveContext) != ResponseCodes.OK)
                throw new GM862GPSException("Failed to check if context active");

            // Check if context isn't active
            if (CheckActiveContext.IndexOf("\r\n#GPRS: 1") == -1)
            {
                if (ExecuteCommand("AT#GPRS=1", 5000) != GM862GPS.ResponseCodes.OK)
                    throw new GM862GPSException("Failed to activate GPRS context");
            }

            // Setup Socket Config
            if (ExecuteCommand("AT#SCFG=1,1,64,90,600,1", 1000) != GM862GPS.ResponseCodes.OK)
                throw new GM862GPSException("Failed to setup Socket config");
        }


        #endregion

        #region Misc functions

        /// <summary>
        /// Request HTML page from Server
        /// !!! UNFINISHED !!!
        /// </summary>
        /// <param name="SocketID">Socket ID to use</param>
        /// <param name="Host">Host of server</param>
        /// <param name="Path">Path to fetch data from</param>
        /// <returns>Recieved HTTP Response</returns>
        public String RequestHTML(int SocketID, String Host, String Path)
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            // Say we want to connect to Socket
            if (ExecuteCommand("AT#SD=" + SocketID.ToString() + ", 0,80,\"" + Host + "\",0,0,0", 50000) != ResponseCodes.CONNECT)
            {
                throw new GM862GPSException("Did not recieve CONNECT response");
            }

            // Object used for recieving HTML
            String RecievedData = "";

            // Build request
            String Request = "GET " + Path + " HTTP/1.1\r\n";
            Request += "Host: " + Host + "\r\n";
            Request += "Connection: Close\r\n";
            Request += "\r\n";

            // Send request
            byte[] OutBuffer = System.Text.Encoding.UTF8.GetBytes(Request);
            lock (_ComPort) { _ComPort.Write(OutBuffer, 0, OutBuffer.Length); }

            // Recieve data
            while (true)
            {
                // Wait for .5 sec for data to arrive
                if (!WaitForDataToArrive.WaitOne(500, true))
                {
                    // When exit recieved exit now
                    if (RecievedData.IndexOf("\r\nNO CARRIER\r\n") != -1)
                    {
                        RecievedData = RecievedData.Substring(0, RecievedData.LastIndexOf("\r\nNO CARRIER\r\n"));
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        break;
                    }

                    // No exit? Give some more time to get data
                    if (!WaitForDataToArrive.WaitOne(10000, true))
                    {
                        // If still no data Escape data mode
                        try { Escape(); }
                        catch { lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; } }

                        // Close socket
                        ExecuteCommand("AT#SH=" + SocketID.ToString(), 1000);
                        break;
                    }
                }

                // Add data to recieve buffer
                lock (_SerialFIFO_CommandMode)
                {
                    RecievedData += _SerialFIFO_CommandMode;
                    _SerialFIFO_CommandMode = "";
                }
            }

            // Return HTTP response
            return RecievedData;
        }


        /// <summary>
        /// Get GPS Location and Fix Data 
        /// </summary>
        /// <param name="Fix">Dimensions of Fix (0 = No fix, 2 = 2D fix, 3 = 3D fix)</param>
        /// <param name="NoSatelites">Number of tracked satelites (Valid when fix>0)</param>
        /// <param name="Latitude">Latitude in degrees (Valid when fix>0)</param>
        /// <param name="Longitude">Longitude in degrees (Valid when fix>0)</param>
        public void ReadGPS(out byte Fix, out byte NoSatelites, out Double Latitude, out Double Longitude)
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            // Send Command
            String GPSResponseBody = "";
            if (ExecuteCommand("AT$GPSACP", 1000, out GPSResponseBody) != ResponseCodes.OK)
                throw new Exception("Failed to get GPS information");

            // Parse response
            int GPSResponseStart;
            int GPSResponseEnd;
            GPSResponseStart = GPSResponseBody.IndexOf("$GPSACP: ");
            if (GPSResponseStart == -1)
                throw new GM862GPSException("Unkown $GPSACP response!");
            GPSResponseStart += "$GPSACP: ".Length;
            GPSResponseEnd = GPSResponseBody.IndexOf("\r\n", GPSResponseStart);
            if (GPSResponseEnd == -1)
                throw new GM862GPSException("Unkown $GPSACP response!");
            GPSResponseBody = GPSResponseBody.Substring(GPSResponseStart, GPSResponseEnd - GPSResponseStart);

            // Split message on comma
            String[] SplitResponse = GPSResponseBody.Split(new char[] { ',' });

            // Check for Fix
            Fix = (byte)_intval(SplitResponse[5]);
            if (Fix > 0)
            {
                // If Fix get Satelites, Lat and Lon
                NoSatelites = (byte)_intval(SplitResponse[10]);
                Latitude = _decodeDMS(SplitResponse[1]);
                Longitude = _decodeDMS(SplitResponse[2]);
            }
            else
            {
                // If not return 0
                NoSatelites = 0;
                Latitude = 0F;
                Longitude = 0F;
            }
        }

        #endregion

        #region Read/Send SMS Text Messages

        /// <summary>
        /// Read Message from Memory
        /// </summary>
        /// <param name="Memory">Memory to read from</param>
        /// <param name="Location">Location of message</param>
        /// <returns>SMS Message</returns>
        public SMSMessage ReadMessage(String Memory, int Location)
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            String ResponseBody;
            int HeaderStart;
            int HeaderEnd;
            int MessageStart;
            int MessageEnd;

            String Status = "";
            String Orginator = "";
            String ArrivalTime = "";
            String Message = "";

            // Select Memory location
            if (ExecuteCommand("AT+CPMS=" + Memory, 15000) != GM862GPS.ResponseCodes.OK)
                throw new Exception("Failed to set SMS Storage");

            // Read Message from Location
            GM862GPS.ResponseCodes p = ExecuteCommand("AT+CMGR=" + Location.ToString(), 1000, out ResponseBody);

            /*if (ExecuteCommand("AT+CMGR=" + Location.ToString(), 1000, out ResponseBody) != GM862GPS.ResponseCodes.OK)
                throw new Exception("Failed to read SMS Storage");
             * */
            if (p != ResponseCodes.ERROR)
            {
                // Make sure there is no unsolicitated text in our message
                if (ResponseBody.IndexOf("\r\n\r\n") != -1)
                    ResponseBody = ResponseBody.Substring(0, ResponseBody.IndexOf("\r\n\r\n") + 2);

                // Check response 
                HeaderStart = ResponseBody.IndexOf("+CMGR: ");
                if (HeaderStart == -1)
                    throw new GM862GPSException("Malformed +CMGR response");
                HeaderStart += "+CMGR: ".Length;
                HeaderEnd = ResponseBody.IndexOf("\r\n", HeaderStart);
                if (HeaderEnd == -1)
                    throw new GM862GPSException("Malformed +CMGR response");

                // Skip <cr><lf>
                MessageStart = HeaderEnd + 2;
                MessageEnd = ResponseBody.Length;
                if (MessageEnd == -1)
                    throw new GM862GPSException("Malformed +CMGR response");

                Message = ResponseBody.Substring(MessageStart, (MessageEnd - MessageStart) - 2);

                // Break up header
                System.Collections.ArrayList Header = new System.Collections.ArrayList();
                String HeaderPart = "";
                bool WithinQuote = false;
                foreach (char c in ResponseBody.Substring(HeaderStart, HeaderEnd + 2 - HeaderStart))
                {
                    if (c == '"') { WithinQuote = !WithinQuote; continue; }
                    if (WithinQuote) { HeaderPart += c; continue; }
                    if (c == ' ') { continue; }
                    if (c == ',') { Header.Add(HeaderPart); HeaderPart = ""; continue; }
                    if ((c == '\r') | (c == '\n')) { Header.Add(HeaderPart); HeaderPart = ""; break; }
                }

                // Return message
                Status = (String)Header[0];
                Orginator = (String)Header[1];
                ArrivalTime = (String)Header[3];


                //*************** Delete SMS *******************
                ExecuteCommand("AT+CMGD=" + Location.ToString(), 15000, out ResponseBody);

                return new SMSMessage(Memory, Location, Status, Orginator, ArrivalTime, Message);
            }
            else
                return new SMSMessage("", 0, "", "", "", "");
        }


        /// <summary>
        /// Send SMS Message
        /// </summary>
        /// <param name="Destination">Destination for SMS</param>
        /// <param name="Message">Message to Send</param>
        public void SendSMSMessage(String Destination, String Message)
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            // Add format identification to destination
            if (Destination.IndexOf('+') != -1)
                Destination = "\"" + Destination + "\",157"; // Internation format
            else
                Destination = "\"" + Destination + "\",129"; // National format

            // Say we want to send message
            if (ExecuteCommand("AT+CMGS=" + Destination, 1000) != ResponseCodes.SEND_SMS_DATA)
                throw new GM862GPSException("Did not recieve SEND_SMS_DATA response");

            // Send Message, End with ^Z
            byte[] OutBuffer = System.Text.Encoding.UTF8.GetBytes(Message + "\x1A");
            lock (_ComPort) { _ComPort.Write(OutBuffer, 0, OutBuffer.Length); }

            // Now we wait for OK response
            int ResponseStart;
            int ResponseLength;
            if (WaitForResponse(out ResponseStart, out ResponseLength, 10000) != ResponseCodes.OK)
                throw new GM862GPSException("Failed to send SMS");

        }

        #endregion

        #region Registration/PIN Checks

        /// <summary>
        /// Checks if connected to a network.
        /// </summary>
        /// <param name="AllowRoaming">Also return true on roaming networks</param>
        /// <returns></returns>
        public bool RegisteredOnNetwork(bool AllowRoaming)
        {
            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            String ResponseBody = "";
            int ResponseStart;
            int ResponseEnd;

            // Do Network Registration request
            if (ExecuteCommand("AT+CREG?", 2500, out ResponseBody) != ResponseCodes.OK)
                throw new GM862GPSException("Network Registration Check Failed");

            // Check response
            ResponseStart = ResponseBody.IndexOf("+CREG: ");
            if (ResponseStart == -1)
                throw new GM862GPSException("Malformed +CREG? response");
            ResponseStart += "+CREG: ".Length;
            ResponseEnd = ResponseBody.IndexOf("\r\n", ResponseStart);
            if (ResponseEnd == -1)
                throw new GM862GPSException("Malformed +CREG? response");

            // ResponseBody now holds requested Network Registration
            ResponseBody = ResponseBody.Substring(ResponseStart, ResponseEnd - ResponseStart);

            // Parse Response
            switch (_intval(ResponseBody.Substring(ResponseBody.LastIndexOf(','))))
            {
                case 1:
                    return true;  // Home network
                case 5:
                    if (AllowRoaming) return true;  // Roaming
                    break;
            }

            // No network
            return false;
        }


        /// <summary>
        /// Checks if connected to a GPRS network.
        /// </summary>
        /// <param name="AllowRoaming">Also return true on roaming networks</param>
        /// <returns></returns>
        public bool RegisteredOnGPRS(bool AllowRoaming)
        {
            // Check if disposed
            //if (_Disposed) throw new ObjectDisposedException();

            String ResponseBody = "";
            int ResponseStart;
            int ResponseEnd;

            // Do Network Registration request
            if (ExecuteCommand("AT+CGREG?", 2500, out ResponseBody) != ResponseCodes.OK)
                throw new GM862GPSException("Network Registration Check Failed");

            // Check response
            ResponseStart = ResponseBody.IndexOf("+CGREG: ");
            if (ResponseStart == -1)
                throw new GM862GPSException("Malformed +CGREG? response");
            ResponseStart += "+CGREG: ".Length;
            ResponseEnd = ResponseBody.IndexOf("\r\n", ResponseStart);
            if (ResponseEnd == -1)
                throw new GM862GPSException("Malformed +CGREG? response");

            // ResponseBody now holds requested Network Registration
            ResponseBody = ResponseBody.Substring(ResponseStart, ResponseEnd - ResponseStart);

            // Parse Response
            switch (_intval(ResponseBody.Substring(ResponseBody.LastIndexOf(','))))
            {
                case 1:
                    return true;  // Home network
                case 5:
                    if (AllowRoaming) return true;  // Roaming
                    break;
            }

            // No network
            return false;
        }


        /// <summary>
        /// Check if there is an outstanding PIN request
        /// If so try to get PIN from getRequestedPIN() funtion
        /// </summary>
        /// <returns>True when all codes are entered</returns>
        private bool CheckPIN()
        {
            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            String ResponseBody = "";
            int RequestTypeStart;
            int RequestTypeEnd;

            // Do PIN request
            if (ExecuteCommand("AT+CPIN?", 2500, out ResponseBody) != ResponseCodes.OK)
            {
                throw new GM862GPSException("PIN Request Check Failed");
            }

            // Check response
            RequestTypeStart = ResponseBody.IndexOf("+CPIN: ");
            if (RequestTypeStart == -1)
                throw new GM862GPSException("Malformed +CPIN? response");
            RequestTypeStart += "+CPIN: ".Length;
            RequestTypeEnd = ResponseBody.IndexOf("\r\n", RequestTypeStart);
            if (RequestTypeEnd == -1)
                throw new GM862GPSException("Malformed +CPIN? response");

            // ResponseBody now holds requested PIN type
            ResponseBody = ResponseBody.Substring(RequestTypeStart, RequestTypeEnd - RequestTypeStart);

            // No more PIN request
            if (ResponseBody == "READY")
                return true;

            // Check for a getRequestedPIN handler
            if (getRequestedPIN == null)
                throw new GM862GPSException("No PIN Request Handler");

            // Send PIN code
            if (ExecuteCommand("AT+CPIN=" + getRequestedPIN(ResponseBody), 2500, out ResponseBody) != ResponseCodes.OK)
                throw new GM862GPSException("Enter PIN Failed");

            // Check for next PIN request
            return false;
        }
        #endregion

        #region Unsolicited Response Thread and Events

        /// <summary>
        /// Tread that parses Unsolicited Responses
        /// </summary>
        private void ParseUnsolicitedResponse()
        {
            String UnsolicitedResponse = "";
            int UnsolicitedResponseStart = 0;
            int UnsolicitedResponseEnd = 0;

            // Loop
            while (true)
            {
                // Try to find Unsolicited Response
                UnsolicitedResponse = "";
                lock (_SerialFIFI_IdleMode)
                {
                    // Check if there is data in FIFO
                    if (_SerialFIFI_IdleMode.Length > 4)
                    {
                        // Find start and end
                        UnsolicitedResponseStart = 0;
                        UnsolicitedResponseEnd = _SerialFIFI_IdleMode.IndexOf("\r\n", UnsolicitedResponseStart + 2);
                        if (UnsolicitedResponseEnd != -1)
                        {
                            UnsolicitedResponse = _SerialFIFI_IdleMode.Substring(0, UnsolicitedResponseEnd - UnsolicitedResponseStart + 2);
                            _SerialFIFI_IdleMode = _SerialFIFI_IdleMode.Substring(UnsolicitedResponseEnd);
                        }
                        else
                        {
                            Thread.Sleep(250);
                            continue;
                        }
                    }
                    else
                    {
                        Thread.Sleep(250);
                        continue;
                    }
                }

                // Trigger event if set
                if (OnUnsolicitedResponse != null)
                {
                    OnUnsolicitedResponse(UnsolicitedResponse);
                }
            }
        }


        /// <summary>
        /// Check if Unsolicitate response is a New SMS response.
        /// If so it fires a OnRecievedSMS event
        /// </summary>
        /// <param name="Response">Recieve response</param>
        public void CheckForNewSMS(string Response)
        {

            // Check for +CMTI: in string
            if (Response.IndexOf("+CMTI: ") == -1)
                return;

            int ResponseStart;
            int ResponseEnd;

            // Select only <mem>, <id>
            ResponseStart = Response.IndexOf("+CMTI: ");
            if (ResponseStart == -1)
                return;
            ResponseStart += "+CMTI: ".Length;
            ResponseEnd = Response.IndexOf("\r\n", ResponseStart);
            if (ResponseEnd == -1)
                return;
            Response = Response.Substring(ResponseStart, ResponseEnd - ResponseStart);

            // SMS event
            if (OnRecievedSMS != null)
                OnRecievedSMS(Response.Substring(0, Response.IndexOf(',')), _intval(Response.Substring(Response.IndexOf(',') + 1)));
        }

        /// <summary>
        /// Check if Unsolicitate response is a Recieving Call response.
        /// If so it fires a OnRecievingCall event
        /// </summary>
        /// <param name="Response">Recieve response</param>
        private void CheckForCall(string Response)
        {
            // Check for RING
            if (Response.IndexOf("\r\nRING\r\n") == -1)
                return;

            // Call event
            if (OnRecievingCall != null)
                OnRecievingCall();
        }

        #endregion

        #region Basic Command Excecution and Handling

        /// <summary>
        /// Execute AT Command. Wait until response is recieved.
        /// </summary>
        /// <param name="Command">Command to execute</param>
        /// <param name="Timeout">Timeout for response</param>
        /// <returns>Recieved response code</returns>
        public ResponseCodes ExecuteCommand(String Command, int Timeout)
        {
            String DummyResponseBody;
            return ExecuteCommand(Command, Timeout, out DummyResponseBody);
        }

        /// <summary>
        /// Execute AT Command. Wait until response is recieved.
        /// </summary>
        /// <param name="Command">Command to execute</param>
        /// <param name="Timeout">Timeout for response in miliseconds</param>
        /// <param name="ResponseBody">Returns the Response body</param>
        /// <returns>Recieved response code</returns>
        public ResponseCodes ExecuteCommand(String Command, int Timeout, out String ResponseBody)
        {
            /*
             * Send Command
             * Wait until command is send
             * Wait for last unsolicited response text is recieved
             * Go to command mode
             * Send \r\n to execute command
             * Wait until \r\n is send
             * Wait for response
             * Save command response body
             * Return response
            */

            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            // Recieved response code
            ResponseCodes RecievedResponse;

            // Check if we are ready for Command
            lock (_CurrentStateLock)
            {
                if (_CurrentState != GM862GPSStates.Idle)
                    throw new GM862GPSException(GM862GPSException.NOT_IN_IDLE_STATE);
            }

            // Send AT to Serial Port 
            byte[] OutBuffer = System.Text.Encoding.UTF8.GetBytes(Command);
            lock (_ComPort)
            {
                _ComPort.Write(OutBuffer, 0, OutBuffer.Length);

                // Wait until command is send
                while (_ComPort.BytesToWrite > 0)
                {
                    WaitForDataToArrive.WaitOne(10, false);
                }
            }

            // Check if in Idle State and set Command state
            lock (_CurrentStateLock)
            {
                if (_CurrentState != GM862GPSStates.Idle)
                    throw new GM862GPSException(GM862GPSException.NOT_IN_IDLE_STATE);

                // Now where in command state
                _CurrentState = GM862GPSStates.Command;
            }

            // Wait for last data to arrive
            // WaitOne returns true when no data recieved in given time
            // So this loop exits when for the last 10 mSec no data has arrived
            while (WaitForDataToArrive.WaitOne(10, false)) { }

            // Clear input buffer
            lock (_SerialFIFO_CommandMode)
            {
                _SerialFIFO_CommandMode = "";
            }


            // Send \r\n
            OutBuffer = System.Text.Encoding.UTF8.GetBytes("\r\n");
            lock (_ComPort)
            {
                _ComPort.Write(OutBuffer, 0, OutBuffer.Length);

                // Wait until send
                while (_ComPort.BytesToWrite > 0)
                {
                    WaitForDataToArrive.WaitOne(10, false);
                }
            }

            // Now we wait for a response to come in
            int ResponseStart=0;
            int ResponseLength;
            RecievedResponse = WaitForResponse(out ResponseStart, out ResponseLength, Timeout);


                // Now get response body and delete response from FIFO
                lock (_SerialFIFO_CommandMode)
                {
                    ResponseBody = _SerialFIFO_CommandMode.Substring(0, ResponseStart);
                    _SerialFIFO_CommandMode = _SerialFIFO_CommandMode.Substring(ResponseStart + ResponseLength);
                }

            // Return recieved response
            return RecievedResponse;
        }


        /// <summary>
        /// Send Escape Sequence '+++'
        /// </summary>
        /// <returns>Return code</returns>
        public ResponseCodes Escape()
        {
            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            ResponseCodes RecievedResponse;
            int ResponseStart;
            int ResponseLength;

            // Set Command state
            lock (_CurrentStateLock)
            {
                // Now where in command state
                _CurrentState = GM862GPSStates.Command;
            }

            Thread.Sleep(2500);

            // Send Command to Serial Port
            byte[] OutBuffer = System.Text.Encoding.UTF8.GetBytes("+++");
            lock (_ComPort) { _ComPort.Write(OutBuffer, 0, OutBuffer.Length); }

            Thread.Sleep(2500);

            // Now we wait for a response to come in
            RecievedResponse = WaitForResponse(out ResponseStart, out ResponseLength, 5000);

            // Delete response from FIFO
            lock (_SerialFIFO_CommandMode)
            {
                _SerialFIFO_CommandMode = _SerialFIFO_CommandMode.Substring(ResponseStart + ResponseLength);
            }

            // Return recieved response
            return RecievedResponse;
        }


        /// <summary>
        /// Waits until it recieves a valid response code. 
        /// This command automaticly sets the state to IDLE or DATA
        /// Throws exception when no response is recieved within timeout time.
        /// </summary>
        /// <param name="ResponseStart">Returns the position in the FIFO of response</param>
        /// <param name="ResponseLength">Returns the length of the response</param>
        /// <param name="Timeout">Timeout in miliseconds</param>
        /// <returns>Received response code</returns>
        private ResponseCodes WaitForResponse(out int ResponseStart, out int ResponseLength, int Timeout)
        {
            // Check if disposed
           // if (_Disposed) throw new ObjectDisposedException();

            // Set TimeOut
            DateTime TimeOutAt = DateTime.Now.AddMilliseconds(Timeout);

            // Check if in Command or Data State
            lock (_CurrentStateLock)
            {
                if ((_CurrentState != GM862GPSStates.Command) && (_CurrentState != GM862GPSStates.Data))
                    throw new GM862GPSException(GM862GPSException.NOT_IN_COMMAND_OR_DATA_STATE);
            }

            // Search for response until timeout
            while (DateTime.Now < TimeOutAt)
            {
                // TODO: 
                // Check if it's better to release/aquire lock for each response check
                lock (_SerialFIFO_CommandMode)
                {
                    //DebugMessage("Check");

                    // OK response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nOK\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nOK\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.OK;
                    }

                    // ERROR response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nERROR\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nERROR\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.ERROR;
                    }

                    // ERROR response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("+CMS ERROR: 321");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "+CMS ERROR: 321".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.ERROR;
                    }
                    

                    // RING response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nRING\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nRING\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.RING;
                    }

                    // BUSY response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nBUSY\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nBUSY\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.BUSY;
                    }

                    // NO CARRIER response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nNO CARRIER\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nNO CARRIER\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.NO_CARRIER;
                    }

                    // NO DIALTONE response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nNO DIALTONE\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nNO DIALTONE\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.NO_DIALTONE;
                    }

                    // NO ANSWER response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\nNO ANSWER\r\n");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\nNO ANSWER\r\n".Length;
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
                        return ResponseCodes.NO_ANSWER;
                    }

                    // CONNECT response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("CONNECT");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = _SerialFIFO_CommandMode.IndexOf("\r\n", ResponseStart + 2);
                        if (ResponseLength != -1)
                        {
                            ResponseLength = (ResponseLength - ResponseStart) + 2;
                            WaitForDataToArrive.Reset();
                            lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Data; }
                            return ResponseCodes.CONNECT;
                        }
                    }

                    // SEND_SMS_DATA (>) response
                    ResponseStart = _SerialFIFO_CommandMode.IndexOf("\r\n>");
                    if (ResponseStart != -1)
                    {
                        ResponseLength = "\r\n>".Length;
                        WaitForDataToArrive.Reset();
                        lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Data; }
                        return ResponseCodes.SEND_SMS_DATA;
                    }
                }

                WaitForDataToArrive.WaitOne(50, false);
            }

            // Response Timeout
            lock (_CurrentStateLock) { _CurrentState = GM862GPSStates.Idle; }
            ResponseStart = -1;
            ResponseLength = 0;
            return ResponseCodes.ERROR;

        }

        #endregion

        #region Internal functions

        /// <summary>
        /// Serial Data In Event
        /// </summary>
        private void _ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            String NewText = "";
            byte[] inBuffer = null;

            if (e.EventType == SerialData.Chars)
            {
                // Lock ComPort and Recieve data
                lock (_ComPort)
                {
                    inBuffer = new byte[_ComPort.BytesToRead];
                    _ComPort.Read(inBuffer, 0, inBuffer.Length);
                }

                try
                {
                    NewText = new String(System.Text.Encoding.UTF8.GetChars(inBuffer));
                }
                catch { return; }

                lock (_CurrentStateLock)
                {

                    if (_CurrentState == GM862GPSStates.Idle)
                    {
                        lock (_SerialFIFI_IdleMode)
                        {
                            _SerialFIFI_IdleMode += NewText;
                        }
                    }
                    else
                    {
                        lock (_SerialFIFO_CommandMode)
                        {
                            _SerialFIFO_CommandMode += NewText;
                        }

                        WaitForDataToArrive.Set();
                    }
                }




            }
        }

        /// <summary>
        /// Internal function to convert a numeric string to an integer
        /// </summary>
        /// <param name="S">String to convert</param>
        /// <returns>Converted integer</returns>
        private static int _intval(String S)
        {
            int r = 0;
            foreach (Char c in S)
            {
                if ("0123456789".IndexOf(c) != -1)
                {
                    r *= 10;
                    r += (int)(c - '0');
                }
            }
            return r;
        }

        /// <summary>
        /// Internal function used to convert DD.MM.SSSS string to degrees
        /// </summary>
        /// <param name="S">Latitude/Longitude in DD.MM.SSSS format</param>
        /// <returns>Latitude/Longitude in degrees</returns>
        private static double _decodeDMS(String S)
        {
            // Internal values used to store values
            double Result = 0F;
            double Degrees = 0F;
            double Minutes = 0F;
            double Seconds = 0F;

            // Internal values used to convert value to double
            bool afterDot = false;
            bool negativeR = false;

            // Go trough String 
            foreach (Char c in S)
            {
                // If Number then add it to R
                if ("0123456789".IndexOf(c) != -1)
                {
                    if (!afterDot)
                    {
                        Degrees *= 10F;
                        Degrees += (double)(c - '0');
                    }
                    else
                    {
                        Seconds *= 10F;
                        Seconds += (double)(c - '0');
                    }
                }
                // Check for Dot
                else if (c == '.')
                {
                    afterDot = true;
                    continue;
                }
                // If West and South Negative number
                else if ((c == 'W') || (c == 'S'))
                {
                    negativeR = true;
                    continue;
                }
                // If North/East ignore 
                else if ((c == 'N') || (c == 'E'))
                {
                    continue;
                }
                // Other character, throw error
                else
                {
                    throw new ArgumentException("Unkown character '" + c + "' in DMS String");
                }
            }

            // Now convert from DD.MMSSSS to Degrees
            Minutes = Degrees % 100F;
            Degrees = System.Math.Floor(Degrees / 100F);
            while (Seconds > 100F)
                Seconds /= 10F;
            Result = Degrees + (Minutes / 60F) + (Seconds / 3600F);

            // Make it negative when in West or South    
            if (negativeR)
                Result = -Result;

            return Result;
        }

        #endregion

    }
}

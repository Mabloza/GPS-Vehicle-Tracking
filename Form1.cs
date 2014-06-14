using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.Collections;
using System.Threading;
using System.Data.SqlClient;
using Microsoft.VisualBasic;
using GsmComm.PduConverter;
using GsmComm.GsmCommunication;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using MapPoint;

namespace SMS
{
    public partial class Form1 : Form
    {
        public string CURRENT_DRIVER_PROFILE;

        private AutoResetEvent receiveNow;
        public SerialPort portf;

        public bool StopRcvSMS;
        public bool StartSend;

        public smartrack[] sm_array= null;
        public smartrack[] sm_array_temp = null;

        SerialCOMPort port;
        frmMap frmSatelliteMap;
        public frmMapPoint frmMapPoint;
        public frmAllarm frmAllarm;
        public frmObiettivo frm_obiettivo;
        string oldErrorMessage;
        ArrayList numbers = new ArrayList();
        public string connectionString = "Server=lab2\\sqlexpress;database=Track;Integrated Security=SSPI;";
        string path = AppDomain.CurrentDomain.BaseDirectory;
        public int Mode;
        int oldMode;
        bool stopped = false;

        //*******  VARIABILI UTILIZZATE PER LA CONNESSIONE IN TCP
        string IP_SERVER = "127.0.0.1";
        string IP_PORT = "4900";
        Dictionary<string, NetworkStream> Connessioni = new Dictionary<string, NetworkStream>();

        public MapPoint.DataSet FLOTTA;

        public string COM_PORT;
        public string COM_SPEED;
        public string COM_TIMEOUT;
        public string SMS_C;
        public string RADIUS_BERSAGLIO;
        public string TEMPO_SOSTA;

        private delegate void SetTextCallback(string text);
        private delegate void SetNewAllarm(string DateTime, string Number, string Message);
        private delegate int GetIndexCallback();
        private delegate string retSelectedNumberCallback(int index);
        private delegate void setReceiveBarCallback(bool full);


        //***********************   CHIAMATE JAVASCRIPT *************************************
        public delegate void SetPushpinDelegate(object[] o);
        public void SetPushpin(object[] o)
        {
            lock (this)
            {
                frmSatelliteMap.satelliteMap.Document.InvokeScript("AddPushPinOnMap", o);
            }
        }

        public delegate void ClearMapDelegate();
        public void ClearMap()
        {
            lock (this)
            {
                frmSatelliteMap.satelliteMap.Document.InvokeScript("ClearMap");
            }
        }

        public delegate void GoToPointDelegate(object[] o);
        public void GoToPoint(object[] o)
        {
            lock (this)
            {
                frmSatelliteMap.satelliteMap.Document.InvokeScript("getRouteToPoint", o);
            }
        }


        public delegate void SetObjectDelegate(object[] o);
        public void SetObject(object[] o)
        {
            lock (this)
            {
                frmSatelliteMap.satelliteMap.Document.InvokeScript("AddPolygonPinOnMap", o);
            }
        }

        public delegate void getDistanceToPointDelegate(object[] o);
        public void getDistanceToPoint(object[] o)
        {
            lock (this)
            {
                frmSatelliteMap.satelliteMap.Document.InvokeScript("getDistanceToPoint", o);
            }
        }
        //*********************************************************************************************



        public Form1(string LVL)
        {
            InitializeComponent();
            if (LVL != null)
            {
                if (LVL.Substring(0, 1) == "0")
                {
                    datiToolStripMenuItem1.Enabled = false;
                    utentiToolStripMenuItem.Enabled = false;

                    if (LVL.Substring(1, 1) == "1")
                        programmazioneToolStripMenuItem.Enabled = true;
                    else
                        programmazioneToolStripMenuItem.Enabled = false;           //programmazioni

                    if (LVL.Substring(2, 1) == "1")
                        reportToolStripMenuItem.Enabled = true;
                    else
                        reportToolStripMenuItem.Enabled = false;  //report
                    if (LVL.Substring(3, 1) == "1")
                    {
                        contextMenuStrip1.Enabled = true;
                        deviceCtrl1.Enabled = true;
                    }
                    else
                    {
                        contextMenuStrip1.Enabled = false;
                        deviceCtrl1.Enabled = false;
                    }           //gestione mezzi
                }
            }
        }

        public void saveNewConnectionString(string str)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(path + "server.txt"))
                File.Delete(path + "server.txt");

            using(StreamWriter wr = new StreamWriter(path+"server.txt"))
            {
                wr.WriteLine(str);
            }
            connectionString = str;
        }

        public void updateConnectionString()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(path + "server.txt"))
            {
                string dd;
                using (StreamReader rd = new StreamReader(path + "server.txt"))
                {
                    dd = rd.ReadLine();
                }
                connectionString = dd;
            }
            else
            {
                frmSQLServer frm = new frmSQLServer();
                frm.mainFrm = this;
                frm.ShowDialog();
            }
        }

        public void checkScandenze(bool showForm)
        {
                updateConnectionString();
                SqlConnection myConnection = new SqlConnection(connectionString);
                string today = DateTime.Now.ToShortDateString();
                string SQL = @"SELECT * FROM pp";

                SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
                System.Data.DataSet ds = new System.Data.DataSet();
                da.Fill(ds, "nm");

                for (int i = 0; i < ds.Tables["nm"].Rows.Count; i++)
                {
                    string d = ds.Tables["nm"].Rows[i]["ID"].ToString();
                }
        }

        private string getNumberByDescription(string Description)
        {
            foreach (smartrack sm in sm_array)
            {
                if (sm.Description == Description) return sm.device_number;
            }
            return "";
        }

        public void getPosition(ArrayList numbers)
        {
            updateConnectionString();
            SqlConnection myConnection = new SqlConnection(connectionString);
            string today = DateTime.Now.ToShortDateString();

            for (int i = 0; i < numbers.Count; i++)
                numbers[i] = getNumberByDescription(numbers[i].ToString());

            if(frmSatelliteMap.Visible==true)
                frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);
            if (frmMapPoint.Visible == true)
                clearMapPointMap();

            
            if (numbers.Count > 0)
            {
                string SQL  = @"SELECT     ID, Vehicle, Position, DateTime
                                FROM         Position AS t
                                WHERE     (ID =
                                (SELECT     MAX(ID) AS Expr1
                                FROM          Position AS i
                                WHERE      (Vehicle = t.Vehicle))) AND
                                (Vehicle = '"+ numbers[0] +"'";
                             
                for (int i = 1; i < numbers.Count; i++)
                {
                    if (numbers[i] != null)
                        SQL += " OR Vehicle='" + numbers[i] + "'";
                }

                SQL += @")";

                SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
                System.Data.DataSet ds = new System.Data.DataSet();
                da.Fill(ds, "pos");

                

                if (ds.Tables["pos"].Rows.Count > 0)
                {
                    string position = ds.Tables["pos"].Rows[0]["Position"].ToString();
                    if (position.IndexOf("$GPRMC") > -1)
                        validGPS(position, ds.Tables["pos"].Rows[0]["Vehicle"].ToString(), ds.Tables["pos"].Rows[0]["DateTime"].ToString(), true);
                    if (position.IndexOf("KTA") > -1 || position.IndexOf("POS") > -1)
                        parseKFTString(position, ds.Tables["pos"].Rows[0]["Vehicle"].ToString(), ds.Tables["pos"].Rows[0]["DateTime"].ToString(),true);
                    if (position.IndexOf("\nHP") > -1)   //HP
                        parseCP100stringLoc(position, ds.Tables["pos"].Rows[0]["Vehicle"].ToString(), ds.Tables["pos"].Rows[0]["DateTime"].ToString(), true);

                    for (int i = 1; i < ds.Tables["pos"].Rows.Count; i++)
                    {
                        position = ds.Tables["pos"].Rows[i]["Position"].ToString();
                        if (position.IndexOf("$GPRMC") > -1)
                            validGPS(position, ds.Tables["pos"].Rows[i]["Vehicle"].ToString(), ds.Tables["pos"].Rows[i]["DateTime"].ToString(), false);
                        if (position.IndexOf("KTA") > -1 || position.IndexOf("POS") > -1)
                            parseKFTString(position, ds.Tables["pos"].Rows[i]["Vehicle"].ToString(), ds.Tables["pos"].Rows[i]["DateTime"].ToString(),false);
                            if (position.IndexOf("\nHP") > -1)
                            parseCP100stringLoc(position, ds.Tables["pos"].Rows[i]["Vehicle"].ToString(), ds.Tables["pos"].Rows[i]["DateTime"].ToString(), false);
                    }
                }
            }
        }

        public bool saveNewPosition(string Number,string Position)
        {
            if (Position.IndexOf('#') > -1)         //Coordinate via GPRS
            {
                string[] P = Position.Split('-');
                string Base = @"HP=GPRS=LOCATION=DATE-" + DateTime.Now.ToLongDateString() + "=TIME-" + DateTime.Now.ToLongTimeString() +"=SPD-000=DIR-294= http://maps.google.com/maps?q=" + P[1] + "," + P[2] + "&z=16";
                Position = Base;
            }


            updateConnectionString();
            Position = Position.Replace("'"," ");
            Position = Position.Replace("\"", " ");

            SqlConnection myConnection = new SqlConnection(connectionString);
            myConnection.Open();
            string today = DateTime.Now.ToShortDateString()+ " " + DateTime.Now.ToShortTimeString();
            string SQL = @"INSERT INTO Position(Vehicle,Position,DateTime) VALUES('"+Number+"','"+Position+"','"+today+"');";

            SqlCommand cmd = new SqlCommand(SQL, myConnection);
            if (cmd.ExecuteNonQuery() > 0)
            {
                myConnection.Close();
                return true;
            }
            myConnection.Close();
            return false;
         }

        public void makeLog(string telNumber,string LOG)
        {
            if (!File.Exists(telNumber + ".log"))
            {
                FileStream p = File.Create(telNumber + ".log");
                p.Close();
            }
            StreamWriter SW;
            SW = File.AppendText(telNumber+".log");
            SW.WriteLine(LOG);
            SW.Close();
        }

        public void appendError(string fname, string LOG)
        {
            if (!File.Exists(fname + ".log"))
            {
                FileStream p = File.Create(fname + ".log");
                p.Close();
            }
            StreamWriter SW;
            SW = File.AppendText(fname + ".log");
            SW.WriteLine(LOG);
            SW.Close();
        }

        public void Output(string text)
        {
            if (this.log.InvokeRequired)
            {
                SetTextCallback stc = new SetTextCallback(Output);
                this.Invoke(stc, new object[] { text });
            }
            else
            {
                log.Text += (text);
                log.Text += ("\r\n");
            }
        }

        private void Output(string text, params object[] args)
        {
            string msg = string.Format(text, args);
            Output(msg);
        }

        public void Allarm(string DateTime,string Number,string Message)
        {
            if (this.log.InvokeRequired)
            {
                SetNewAllarm stc = new SetNewAllarm(Allarm);
                this.Invoke(stc, new object[] { DateTime,Number,Message });
            }
            else
            {
                frmAllarm.addNewAllarm(DateTime, Number, Message);
            }
        }

        private void Allarm(string DateTime, string Number, string Message, params object[] args)
        {
            string DT = string.Format(DateTime, args);
            string Num = string.Format(Number, args);
            string Mess = string.Format(Message, args);
            Allarm(DT,Num,Mess);
        }


        public double Convert_To_Decimal(string Coord)        
        {            
            if(Coord.IndexOf(',')>-1)
            {
                string[] h = Coord.Split(',');
                Coord = h[0];
            }   
            //"77 2 00.000W"; Sample Input from textBox1
            string input = Coord;            
            double sd = 0.0;            
            double min = 0.0;           
            double sec = 0.0;            
            double deg = 0.0;
            string direction = input.Substring((input.Length - 1), 1);
            string sign = "";            
            if ((direction.ToUpper() == "S") || (direction.ToUpper() == "W"))
            {                
                sign = "-";            
            }                      
            string[] arr = input.Split(new char[] { ' ' });
            min = Convert.ToDouble(arr[1]);            
            string[] arr1 = arr[2].Split(new char[] { '.' });
            sec = Convert.ToDouble(arr1[0]);            
            deg = Convert.ToDouble(arr[0]);            
            min = min / ((double)60);            
            sec = sec / ((double)3600);            
            sd = deg + min + sec;            
            if (!(string.IsNullOrEmpty(sign)))            
            {                
                sd = sd * (-1);            
            }            
            sd = Math.Round(sd, 6);            
            string sdnew = Convert.ToString(sd);            
            string sdnew1 = "";            
            sdnew1 = string.Format("{0:0.000000}", sd);

            return Double.Parse(sdnew1);
            //EXPECTED OUTPUT -77.03333        
        }

        public void clearMapPointMap()
        {
            MapPoint.Map ActiveMap = frmMapPoint.Map.ActiveMap;
            try
            {
                if (ActiveMap != null)
                {
                    foreach (MapPoint.DataSet dataSet in ActiveMap.DataSets)
                    {
                        if (dataSet.RecordCount > 0 && (dataSet.DataMapType == MapPoint.GeoDataMapType.geoDataMapTypePushpin || dataSet.DataMapType == MapPoint.GeoDataMapType.geoDataMapTypeTerritory))
                        {

                            dataSet.Delete();
                        }
                    }
                }
            }
            catch { }
        }
        public void SetVehiclePosition(double Latitude, double Longitude, Map ActiveMap, string Name, string Descrizione, string ID, string IconPath,bool Clear)
        {
            Symbol VehicleSymbols = null;
           // string SymbolPath;
           // int SymbolID;
            Symbol NullSymbols = null;
            Location Position;
            Pushpin vehicle;

            if (Clear)
            {
                if (ActiveMap != null)
                {
                    foreach (MapPoint.DataSet dataSet in ActiveMap.DataSets)
                    {
                        if (dataSet.RecordCount > 0 && (dataSet.DataMapType == MapPoint.GeoDataMapType.geoDataMapTypePushpin || dataSet.DataMapType == MapPoint.GeoDataMapType.geoDataMapTypeTerritory))
                        {
                            dataSet.Delete();
                        }
                    }
                    if(Mode==1)
                        FLOTTA = frmMapPoint.Map.ActiveMap.DataSets.AddPushpinSet("FLOTTA");
                }
            }

            string path = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                if (File.Exists(IconPath))
                    VehicleSymbols = ActiveMap.Symbols.Add(IconPath);
            }
            catch
            {
                VehicleSymbols = ActiveMap.Symbols.ItemByID(0);
            }
            if (File.Exists(@path + "white.bmp"))
                NullSymbols = ActiveMap.Symbols.Add(@path + "white.bmp");

            Position = ActiveMap.GetLocation(Latitude, Longitude, ActiveMap.Altitude);
            vehicle = ActiveMap.AddPushpin(Position, Name);
                
            if(FLOTTA!=null && Mode==1)
                vehicle.MoveTo(FLOTTA);
            
            //vehicle.Symbol = (short)(ActiveMap.Symbols.Count-1);
            if(VehicleSymbols!=null)
                vehicle.Symbol = VehicleSymbols.ID;
            vehicle.Note = Descrizione;
            vehicle.BalloonState = GeoBalloonState.geoDisplayBalloon;

            if(Mode != 1)
              ActiveMap.GoToLatLong(Latitude, Longitude, ActiveMap.Altitude);
        }

        public void validKFTGPS(string _lat, string _lon,string speed,string number, string TimeDate, bool Clear)
        {
            if (!string.IsNullOrEmpty(_lat))
            {
                if (Clear)
                    frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);

                double lat = Convert_To_Decimal(_lat);
                double lon = Convert_To_Decimal(_lon);

                smartrack pp = searchDevice(number);

                //*************************************************************
                //*** CONTROLLARE SE IL MEZZO HA ASSOCIATO UN'OBIETTIVO
                //*** E IN TAL CASO CONTROLLARE SE E' ALL'INTERNO
                //*************************************************************
                if (pp.Bersaglio != null)               //esiste un bersaglio
                {
                    MapUtils mu = new MapUtils(pp, this);
                    Location bers = null;
                    string[] hh = pp.Bersaglio.Split('|');
                    if (!string.IsNullOrEmpty(hh[1]) && !string.IsNullOrEmpty(hh[2]))
                        bers = frmMapPoint.Map.ActiveMap.GetLocation(double.Parse(hh[1]), double.Parse(hh[2]), frmMapPoint.Map.ActiveMap.Altitude);
                    mu.radiusSearch(frmMapPoint.Map.ActiveMap, bers.Latitude, bers.Longitude, lat, lon);
                }

                if (frmSatelliteMap.Visible == true)
                {
                    object[] ob = new object[6];
                    ob[0] = lat;
                    ob[1] = lon;
                    ob[2] = pp.Description + " (" + number + ")";
                    ob[3] = TimeDate;
                    ob[4] = pp.image;
                    ob[5] = true;
                    frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
                }

                if (frmMapPoint.Visible == true)
                {
                    SetVehiclePosition(lat, lon, frmMapPoint.Map.ActiveMap, pp.Description, pp.Targa + " (" + number + ")\r\nVel:"+speed+" Km/h", "", pp.image,Clear);
                }

                pp.Lat = lat;
                pp.Lon = lon;
                pp.lastDate = TimeDate;

                saveDevices(sm_array);
            }
        }

        private void parseKFTString(string kft,string number, string DateTime,bool clear)
        {
            kft = kft.Replace("\n", ",");
            string[] tmp = kft.Split(':');
            string vel="0";

            if (tmp[2].Contains(','))
            {
                string[] g = tmp[2].Split(',');
                tmp[2] = g[0];
            }

            if (tmp[3].Contains(','))
            {
                string[] g = tmp[3].Split(',');
                tmp[3] = g[0];
            }

            if (tmp[7].Contains(','))
            {
                vel = tmp[7].Replace(",", "");
            }

            string lat = tmp[2];//.Substring(0,tmp[2].Length-4);
            string lon = tmp[3];//.Substring(0, tmp[3].Length -4);
            lat = lat.Replace("'"," ");
            lon = lon.Replace("'"," ");
            lat = lat.Replace("\"", " ");
            lon = lon.Replace("\"", " ");
            validKFTGPS(lat, lon, vel,number, DateTime, clear);
        }


        public string returnKFTCoord(string kft)
        {
            string[] tmp = kft.Split(':');
            string lat = tmp[2].Substring(0, tmp[2].Length - 4);
            string lon = tmp[3].Substring(0, tmp[3].Length - 4);
            lat = lat.Replace("'", " ");
            lon = lon.Replace("'", " ");
            lat = lat.Replace("\"", " ");
            lon = lon.Replace("\"", " ");
            double _lat = Convert_To_Decimal(lat);
            double _lon = Convert_To_Decimal(lon);

            return _lat.ToString() + "§" + _lon.ToString();
        }


        public string returnCP100Coord(string Position)
        {
            string[] tmp = Position.Split('=');
            if (tmp.Length < 8) return "";

            string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
            string[] loc = tmp2.Split(',');
            string lat = loc[0];
            string lon = loc[1];

            lat = lat.Substring(1, lat.Length - 1);
            lon = lon.Substring(1, lon.Length - 1);
            lat = lat.Replace('.', ',');
            lon = lon.Replace('.', ',');
            double _lat = double.Parse(lat);
            double _lon = double.Parse(lon);

            return _lat.ToString() + "§" + _lon.ToString();
        }

        private void ShowMessageGM862(ShortMessage data)
        {
                setFullBar(false);
                // Received message
                string toWrite = "";
                toWrite = data.Message;
                string dt = data.Sent;
                if (data.Message.IndexOf("$GPRMC") > -1)
                {
                    validGPS(data.Message, data.Sender, dt, true);
                    Output(dt + " Ricevuto posizione del terminale: " + data.Sender);
                    makeLog(data.Sender, "Ricevuto posizione del terminale: " + data.Sender);
                    saveNewPosition(data.Sender, data.Message);
                }
                if (data.Message.IndexOf("KTA") > -1 || data.Message.IndexOf("POS") > -1 || data.Message.IndexOf("LOGIN") > -1)
                {
                    if (data.Message.IndexOf("LOGIN") > -1)
                    {
                        string login = "Login del dispositivo " + searchDevice(data.Sender).Description + " riuscito.";
                        makeLog(data.Sender, login);
                        frmAllarm.addNewAllarm(dt, data.Sender, login);
                        smartrack sm = searchDevice(data.Sender);
                        Output(dt + " Login del dispositivo: " + sm.Description + " (" + data.Sender + ")");
                    }
                    else
                    {
                        parseKFTString(data.Message, data.Sender, dt, true);
                        Output(dt + " Ricevuto posizione del terminale: " + data.Sender);
                        makeLog(data.Sender, "Ricevuto posizione del terminale: " + data.Sender);
                        saveNewPosition(data.Sender, data.Message);
                        frmAllarm.addNewAllarm(dt, data.Sender, "Ricevuto posizione del terminale: " + data.Sender);
                    }
                }
                if (data.Message.IndexOf("\nHP=") > -1)
                {
                    parseCP100string(data.Message, data.Sender, dt, true);
                    makeLog(data.Sender, "Ricevuto posizione del terminale: " + data.Sender);
                    saveNewPosition(data.Sender, data.Message);
                }
                if (data.Message.IndexOf("VKT%") > -1)
                {
                    string TipoMessaggio = data.Message.Substring(data.Message.LastIndexOf("VKT%") + 4, 4);
                    string[] Mess = data.Message.Split(':');
                    string Messaggio = Mess[Mess.Length - 1];
                    string m = "";
                    m = getMessage(Messaggio);
                    if (m != "")
                    {
                        frmAllarm.addNewAllarm(dt, data.Sender, m);
                    }
                }

                makeLog(data.Sender, toWrite);
                if (Mode == 1)
                    showFlotta();
                return;
        }

        private void ShowMessage(SmsPdu pdu)
        {
            if (pdu is SmsDeliverPdu)
            {
                setFullBar(false);
                // Received message
                SmsDeliverPdu data = (SmsDeliverPdu)pdu;
                string toWrite="";
                toWrite = data.UserDataText;
                string dt = data.SCTimestamp.Day + "/" + data.SCTimestamp.Month + "/" + data.SCTimestamp.Year + " " + data.SCTimestamp.Hour + ":" + data.SCTimestamp.Minute + ":" + data.SCTimestamp.Second;
                if (data.UserDataText.IndexOf("$GPRMC") > -1)
                {
                    validGPS(data.UserDataText, data.OriginatingAddress,dt,true);
                    Output(dt+" Ricevuto posizione del terminale: " + data.OriginatingAddress);
                    makeLog(data.OriginatingAddress, "Ricevuto posizione del terminale: " + data.OriginatingAddress);
                    saveNewPosition(data.OriginatingAddress, data.UserDataText);
                }
                if (data.UserDataText.IndexOf("KTA") > -1)
                {
                    parseKFTString(data.UserDataText, data.OriginatingAddress, dt,true);
                    Output(dt + " Ricevuto posizione del terminale: " + data.OriginatingAddress);
                    makeLog(data.OriginatingAddress, "Ricevuto posizione del terminale: " + data.OriginatingAddress);
                    saveNewPosition(data.OriginatingAddress, data.UserDataText);
                    frmAllarm.addNewAllarm(dt, data.OriginatingAddress, "Ricevuto posizione del terminale: " + data.OriginatingAddress);
                }
                if(data.UserDataText.IndexOf("\nHP=")>-1)
                {
                    parseCP100string(data.UserDataText,data.OriginatingAddress,dt,true);
                    makeLog(data.OriginatingAddress, "Ricevuto posizione del terminale: " + data.OriginatingAddress);
                    saveNewPosition(data.OriginatingAddress, data.UserDataText);
                }
                if (data.UserDataText.IndexOf("VKT%") > -1)
                {
                    string TipoMessaggio = data.UserDataText.Substring(data.UserDataText.LastIndexOf("VKT%")+4, 4);
                    string[] Mess = data.UserDataText.Split(':');
                    string Messaggio = Mess[Mess.Length-1];
                    string m = "";
                    m = getMessage(Messaggio);
                    if (m != "")
                    {
                        frmAllarm.addNewAllarm(dt, data.OriginatingAddress, m);
                    }
                }
                
                makeLog(data.OriginatingAddress, toWrite);
                return;
            }
        }

        private void parseCP100stringLoc(string Position, string number, string DateTime, bool clear)
        {
            string[] tmp = Position.Split('=');
            if (tmp.Length > 3)
            {
                string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
                string[] loc = tmp2.Split(',');
                string lat = loc[0];
                string lon = loc[1];
                validCP100GPS(lat, lon, number, DateTime, clear);
            }
        }

        private void parseCP100string(string Position,string number, string DateTime, bool clear)
        {
                string Osc = "";
                string[] tmp = Position.Split('=');
                if (tmp.Length > 3)
                {
                    if (tmp[3].Substring(5, 1) == "0" && tmp[3].Substring(6, 1) == "0")
                        Osc = " [Cielo oscurato]";
                }
                if (tmp[2] == "LOCATION")
                {
                    string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
                    string[] loc = tmp2.Split(',');
                    string lat = loc[0];
                    string lon = loc[1];
                    validCP100GPS(lat, lon, number, DateTime, clear);
                    if(frmAllarm.Visible==true)
                        frmAllarm.addNewAllarm(DateTime, number, "Ricevuto posizionamento terminale" + Osc);
                }
                else if (tmp[2] == "SOS!")
                {
                    string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
                    string[] loc = tmp2.Split(',');
                    string lat = loc[0];
                    string lon = loc[1];
                    validCP100GPS(lat, lon, number, DateTime, clear);
                    if (frmAllarm.Visible == true)
                        frmAllarm.addNewAllarm(DateTime, number, "*** SOS DISPOSITIVO ***" + Osc);
                }
                else if (tmp[2] == "BATTERY WARNING")
                {
                    string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
                    string[] loc = tmp2.Split(',');
                    string lat = loc[0];
                    string lon = loc[1];
                    validCP100GPS(lat, lon, number, DateTime, clear);
                    if (frmAllarm.Visible == true)
                        frmAllarm.addNewAllarm(DateTime, number, "Batteria quasi scarica" + Osc);
                }
                else if (tmp[2] == "LOWBATTERY")
                {
                    string tmp2 = tmp[8].Substring(0, tmp[8].Length - 2);
                    string[] loc = tmp2.Split(',');
                    string lat = loc[0];
                    string lon = loc[1];
                    validCP100GPS(lat, lon, number, DateTime, clear);
                    if (frmAllarm.Visible == true)
                        frmAllarm.addNewAllarm(DateTime, number, "*** BATTERIA SCARICA ***" + Osc);
                }
                else
                {
                    if (frmAllarm.Visible == true)
                        frmAllarm.addNewAllarm(DateTime, number, tmp[2]);
                }
        }

        public void validCP100GPS(string _lat, string _lon, string number, string TimeDate, bool Clear)
        {
            if (!string.IsNullOrEmpty(_lat))
            {
                if (Clear && frmSatelliteMap.Visible==true)
                    frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);

                _lat = _lat.Substring(1, _lat.Length - 1);
                _lon = _lon.Substring(1, _lon.Length - 1);
                _lat = _lat.Replace('.', ',');
                _lon = _lon.Replace('.', ',');
                double lat = double.Parse(_lat);
                double lon = double.Parse(_lon);

                smartrack pp = searchDevice(number);

                if (frmSatelliteMap.Visible == true)
                {
                    object[] ob = new object[6];
                    ob[0] = lat;
                    ob[1] = lon;
                    ob[2] = pp.Description + " (" + number + ")";
                    ob[3] = TimeDate;
                    ob[4] = pp.image;
                    ob[5] = true;

                    if (frmSatelliteMap.Visible == true)
                        frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
                }

                if (frmMapPoint.Visible == true)
                {
                    SetVehiclePosition(lat, lon, frmMapPoint.Map.ActiveMap, pp.Description, pp.Targa + " (" + number + ")", "", pp.image,Clear);

                    //*************************************************************
                    //*** CONTROLLARE SE IL MEZZO HA ASSOCIATO UN'OBIETTIVO 
                    //*** E IN TAL CASO CONTROLLARE SE E' ALL'INTERNO
                    //*************************************************************
                    if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        Location bers = null;
                        string[] hh = pp.Bersaglio.Split('|');
                        if (!string.IsNullOrEmpty(hh[1]) && !string.IsNullOrEmpty(hh[2]))
                            bers = frmMapPoint.Map.ActiveMap.GetLocation(double.Parse(hh[1]), double.Parse(hh[2]), frmMapPoint.Map.ActiveMap.Altitude);
                        //else
                        //   bers = frmMapPoint.Map.ActiveMap.

                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, bers.Latitude, bers.Longitude, lat, lon);
                    }

                   /* if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, pp.Bersaglio.Latitude, pp.Bersaglio.Longitude, lat, lon);
                    }*/
                }

                pp.Lat = lat;
                pp.Lon = lon;
                pp.lastDate = TimeDate;

                saveDevices(sm_array);
            }
        }


        private string getMessage(string CODE)
        {
            using (StreamReader rd = new StreamReader(path + "Messages.txt"))
            {
                while (!rd.EndOfStream)
                {
                    string[] tmp = rd.ReadLine().Split('\t');
                    if (tmp[0] == CODE) return tmp[1];
                }
            }
            return "";
        }

        private string StatusToString(PhoneMessageStatus status)
        {
            // Map a message status to a string
            string ret;
            switch (status)
            {
                case PhoneMessageStatus.All:
                    ret = "All";
                    break;
                case PhoneMessageStatus.ReceivedRead:
                    ret = "Read";
                    break;
                case PhoneMessageStatus.ReceivedUnread:
                    ret = "Unread";
                    break;
                case PhoneMessageStatus.StoredSent:
                    ret = "Sent";
                    break;
                case PhoneMessageStatus.StoredUnsent:
                    ret = "Unsent";
                    break;
                default:
                    ret = "Unknown (" + status.ToString() + ")";
                    break;
            }
            return ret;
        }


        //************************************** MULTITHREAD CALL *****************************
        public int retSelectedIndex()
        {
            if (this.listBox1.InvokeRequired)
            {
                GetIndexCallback stc = new GetIndexCallback(retSelectedIndex);
                object re = this.Invoke(stc);
                return int.Parse(re.ToString());
            }
            else
            {
                return listBox1.SelectedIndex;
            }
        }

        public int retSelectedIndex(object l)
        {
            return listBox1.SelectedIndex;
        }


        public string retSelectedNumber(int index)
        {
            if (this.listBox1.InvokeRequired)
            {
                retSelectedNumberCallback stc = new retSelectedNumberCallback(retSelectedNumber);
                object re = this.Invoke(stc,new object[] { index });
                return re.ToString();
            }
            else
            {
                return listBox1.Items[index].ToString();
            }
        }

        public string retSelectedNumber(object l)
        {
            return listBox1.Items[listBox1.SelectedIndex].ToString();
        }



        public void setFullBar(bool full)
        {
            if(this.statusStrip1.InvokeRequired)
            {
                setReceiveBarCallback stc = new setReceiveBarCallback(setFullBar);
                this.Invoke(stc, new object[] { full });
            }
            else
            {
                if (full)
                    toolStripProgressBar1.Value = 100;
                else
                    toolStripProgressBar1.Value = 0;
            }
        }

        public void setFullBar(object full)
        {
            if ((bool)full)
                toolStripProgressBar1.Value = 100;
            else
                toolStripProgressBar1.Value = 0;
        }
        //*******************************************************************************************************


        public void checkSMS()
        {
            if (portf != null)
            {
                Output("Servizio di ricezione attivo.");
                while (!stopped)
                {
                    while (StopRcvSMS)
                        StartSend = true;
                    StartSend = false;
                    try
                    {
                        string input = ExecCommand("AT+CMGL=\"ALL\"", 5000, "Failed to read the messages.");
                        ShortMessageCollection messages = ParseMessages(input);
                        Cursor.Current = Cursors.Default;
                        foreach (ShortMessage message in messages)
                        {
                            setFullBar(false);
                            Output(string.Format("Message status = {0}, Location = {1}, From = {2}", message.Status, message.Index, message.Sender));
                            ExecCommand("AT+CMGD=" + message.Index, 5000, "Impossibile cancellare il messaggio");
                            ShowMessageGM862(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (oldErrorMessage != ex.Message)
                        {
                            Output("Errore: " + ex.Message);
                            appendError("error", ex.Message);
                            oldErrorMessage = ex.Message;
                        }
                    }
                }
            }
            else
            {
                Output("Impossibile avviare il servizio di ricezione. Modem non presente.");
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
       
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        public bool saveDevices(smartrack[] sm_array)
        {
            try
            {
                //Save the class
                //Stream myFileStream = File.Create(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + @"\text.txt");
                Stream myFileStream = File.Create(path + @"vehicle");
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(myFileStream, sm_array);
                myFileStream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public smartrack[] loadDevices(SerialCOMPort port)
        {
            try
            {
                smartrack[] restoredAccount;
                //Stream myFileStream = File.OpenRead(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + @"\text.txt");
                Stream myFileStream = File.OpenRead(path + @"vehicle");
                BinaryFormatter deserializer = new BinaryFormatter();
                restoredAccount = (smartrack[])(deserializer.Deserialize(myFileStream));
                myFileStream.Close();

                for (int i = 0; i < restoredAccount.Length; i++)
                {
                    if (restoredAccount[i] != null)
                    {
                        port.mainFrm = this;
                        restoredAccount[i].serialPort = port;
                    }
                }

                return restoredAccount;
            }
            catch
            {
                return null;
            }
        }

        private void comm_MessageReceived(object sender, GsmComm.GsmCommunication.MessageReceivedEventArgs e)
        {
            //MessageReceived();
            //string a = "";
        }

        private string loadSettings()
        {
            if (File.Exists("configuration.inf"))
            {
                using (StreamReader rd = new StreamReader("configuration.inf"))
                {
                    int comport = int.Parse(rd.ReadLine());
                    int speed = int.Parse(rd.ReadLine());
                    int timeout = int.Parse(rd.ReadLine());
                    string smscentre = rd.ReadLine();

                    CommSetting.Comm_Port = comport;
                    CommSetting.Comm_BaudRate = speed;
                    CommSetting.Comm_TimeOut = timeout;
                    CommSetting.comm = new GsmCommMain(comport,speed,timeout);

                    return smscentre;
                }
            }
            return "";
        }

        public void saveOptions(string port, string speed, string timeout, string sms_c,string radius,string sosta_time)
        {
            using (StreamWriter wr = new StreamWriter(path + "options"))
            {
                wr.WriteLine(port);
                wr.WriteLine(speed);
                wr.WriteLine(timeout);
                wr.WriteLine(sms_c);
                wr.WriteLine(radius);
                wr.WriteLine(sosta_time);
            }
            loadOptions();
        }

        public bool loadOptions()
        {
            if(File.Exists(path+"options"))
            {
                using(StreamReader rd = new StreamReader(path+"options"))
                {
                    COM_PORT = rd.ReadLine();
                    COM_SPEED = rd.ReadLine();
                    COM_TIMEOUT = rd.ReadLine();
                    SMS_C = rd.ReadLine();
                    RADIUS_BERSAGLIO = rd.ReadLine();
                    TEMPO_SOSTA = rd.ReadLine();
                    return true;
                }
            }
            return false;
        }

        private void test(string pp)
        {
            //MessageBox.Show(pp);
        }


        /*******************************/

        public SerialPort OpenPort(string portName)
        {
            SerialPort portf = new SerialPort();
            portf.PortName = portName;
            portf.BaudRate = 9600;
            portf.DataBits = 8;
            portf.StopBits = StopBits.One;
            portf.Parity = Parity.None;
            portf.ReadTimeout = 600;
            portf.WriteTimeout = 600;
            portf.Encoding = Encoding.GetEncoding("iso-8859-1");
            portf.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            portf.Open();
            portf.DtrEnable = true;
            portf.RtsEnable = true;
            return portf;
        }
        private void ClosePort(SerialPort port)
        {
            portf.Close();
            portf.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
                receiveNow.Set();
        }

        public string ExecCommandNoReturn(string command, int responseTimeout, string errorMessage)
        {
            try
            {
                portf.DiscardOutBuffer();
                portf.DiscardInBuffer();
                receiveNow.Reset();
                portf.Write(command);

                string input = ReadResponse(responseTimeout);
                if ((input.Length == 0) || (!input.EndsWith(">")))
                    throw new ApplicationException("No success message was received.");
                return input;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(errorMessage, ex);
            }
        }

        public string ExecCommand(string command, int responseTimeout, string errorMessage)
        {
            try
            {
                portf.DiscardOutBuffer();
                portf.DiscardInBuffer();
                receiveNow.Reset();
                portf.Write(command + "\r\n");

                string input = ReadResponse(responseTimeout);
                if ((input.Length == 0) || (!input.Contains("OK")))//(!input.EndsWith("\r\nOK\r\n")))
                {
                    if (input.Contains('>')) return input;
                    else throw new ApplicationException("No success message was received.");
                }
                return input;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(errorMessage, ex);
            }
        }

        public bool sendMsg(string PhoneNo, string Message)
        {
            bool isSend = false;
            string recievedData;
            int tmout = 0;
            try
            {
                StopRcvSMS = true;
                while (StartSend == false)
                {
                    Thread.Sleep(100);
                    tmout++;
                    if (tmout >= 5) break;
                }

                char apici = '"';
                //this.port = OpenPort(strPortName,strBaudRate);
              //  recievedData = ExecCommand("AT", 300, "No phone connected.");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                String command =@"AT+CMGS=" +apici+ PhoneNo +apici;
                recievedData = ExecCommand(command, 300, "Failed to accept phoneNo");
                command = Message + char.ConvertFromUtf32(26)+"\r\n";
                recievedData = ExecCommand(command, 3000, "Failed to send message"); //3 seconds
                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    recievedData = "Message sent successfully";
                    isSend = true;
                }
                else if (recievedData.Contains("ERROR"))
                {
                    string recievedError = recievedData;
                    recievedError = recievedError.Trim();
                    recievedData = "Following error occured while sending the message" + recievedError;
                    isSend = false;
                }
                return isSend;
            }
            catch (Exception ex)
            {
              //  throw new Exception(ex.Message);
                recievedData =ex.Message;
               // ExecCommand("ATZ", 300, "No phone connected.");
               // ExecCommand("AT", 300, "No phone connected.");
              //  StopRcvSMS = false;
              //  StartSend = false;
                return false;
            }
            finally
            {
                StopRcvSMS = false;
                StartSend = false;
            }
        }     

        private string ReadResponse(int timeout)
        {
            string buffer = string.Empty;
            do
            {
                if (receiveNow.WaitOne(timeout, false))
                {
                    string t = portf.ReadExisting();
                    buffer += t;
                }
                else
                {
                    if (stopped) 
                        break;
                    if (buffer.Length <= 0)
                        throw new ApplicationException("No data received from phone.");
                }

                if(buffer.Contains('>')) return buffer;
            }
            while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\nERROR\r\n"));
            return buffer;
        }
		
        private ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
            Match m = r.Match(input);

            string[] mySplit = input.Split(new string[] { "+CMGL:" }, StringSplitOptions.None);
            int num = mySplit.Length - 1;
            for (int i = 1; i <= num; i++)
            {
                string sms = mySplit[i].Replace("\r\nOK\r", "");
                sms = sms.Replace("\r\n", "");
                sms = sms.Replace(",\"", ",");
                sms = sms.Replace("\"", ",");
                sms = sms.Replace("\r\n", ",");
                string[] mess = sms.Split(',');

                if (mess.Length > 4)
                {
                    for (int y = 5; y < mess.Length; y++)
                        mess[4] += ","+mess[y];
                }

                ShortMessage msg = new ShortMessage();
                msg.Index = int.Parse(mess[0]);
                msg.Status = mess[1];
                msg.Sender = mess[3];
                msg.Alphabet = "";
                msg.Sent = DateTime.Now.ToString();
                if (mess[4].IndexOf("\n") > -1) mess[4].Replace("\n", "");
                msg.Message = mess[4];
                messages.Add(msg);
            }

            return messages;
        }
        /*******************************/
        private void Form1_Load(object sender, EventArgs e)
        {    
            if (frmMapPoint == null)
            {
                frmMapPoint = new frmMapPoint();
                frmMapPoint.mainFrm = this;
                frmMapPoint.Show();
                frmMapPoint.loadMap();
            }
            if (frmSatelliteMap == null)
            {
                frmSatelliteMap = new frmMap();
                frmSatelliteMap.satelliteMap.Navigate(@AppDomain.CurrentDomain.BaseDirectory + "map.htm");
                frmSatelliteMap.satelliteMap.Document.InvokeScript("SetMapStyle", new object[] { "VEMapStyle.Hybrid" });
                //frmSatelliteMap.Show();
            }
            if (frmAllarm == null)
            {
                frmAllarm = new frmAllarm();
                frmAllarm.mainFrm = this;
                frmAllarm.Show();
            }

            port = null;
            receiveNow = new AutoResetEvent(false);
            
            if (CommSetting.comm != null && CommSetting.comm.IsOpen())
            {
                CommSetting.comm.Close();
                CommSetting.comm = null;
            }

            if (loadOptions())
            {
                try
                {
                    this.portf = OpenPort("COM" + COM_PORT);
                    Cursor.Current = Cursors.WaitCursor;
                    // Check connection
                    try
                    {
                        ExecCommand("AT", 300, "No phone connected at COM" + COM_PORT + ".");
                    }
                    catch
                    {
                        string command = " " + char.ConvertFromUtf32(26) + "\r";
                        ExecCommand(command, 3000, "Failed to send message"); //3 seconds
                        ExecCommand("AT", 300, "No phone connected");
                    }
                    // Use message format "Text mode"
                    ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                    // Use character set "ISO 8859-1"
                    ExecCommand("AT+CSCS=\"8859-1\"", 300, "Failed to set character set.");
                    // Select SIM storage
                    ExecCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");

                    //sendMsg("3486526983", "Messaggio di prova");                       
                }
                catch(Exception ex)
                {
                    Output(ex.Message);
                }
            }
            else
            {
                options opt = new options();
                opt.mainFrm = this;
                loadOptions();
                opt.textBox1.Text = COM_PORT;
                opt.textBox2.Text = COM_SPEED;
                opt.textBox3.Text = COM_TIMEOUT;
                opt.textBox4.Text = SMS_C;
                opt.ShowDialog();
                 try
                   {
                       this.portf = OpenPort("COM" + COM_PORT);
                       Cursor.Current = Cursors.WaitCursor;
                       // Check connection
                       ExecCommand("AT", 300, "No phone connected at " + "COM20" + ".");
                       // Use message format "Text mode"
                       ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                       // Use character set "ISO 8859-1"
                       ExecCommand("AT+CSCS=\"8859-1\"", 300, "Failed to set character set.");
                       // Select SIM storage
                       ExecCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");

                      // sendMsg("+393486526983", "Messaggio di prova");
                    }
                    catch
                    { }
            }


            port = new SerialCOMPort();

            loadDevices();                                  //Carica i dispositivi da file

            StopRcvSMS = false;
            StartSend = false;

            Thread smsRcv = new Thread(checkSMS);
            smsRcv.Start();

               Thread listen = new Thread(listenClient);
            listen.Start();

            updateConnectionString();
        }

        private void listenSocket()
        {
            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(IP_SERVER), int.Parse(IP_PORT));
            while (this.Visible == true)
            {
                try
                {
                    client.Connect(serverEndPoint);

                    NetworkStream clientStream = client.GetStream();
                    StreamReader rd = null;
                    clientStream.Write(Encoding.Default.GetBytes("HELO"),0,4);
                    while (client.Connected)
                    {
                        if (clientStream.DataAvailable)
                        {
                            rd = new StreamReader(clientStream);
                            string test = rd.ReadLine();
                            //Output(" Ricevuta posizione del terminale: " + test);
                            string[] tmp = test.Split('-');


                            smartrack sm = searchDevicebyDescription(tmp[1]);

                            if (sm != null)
                            {

                                if (frmSatelliteMap.Visible == true)
                                    frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);
                                if (frmMapPoint.Visible == true)
                                    clearMapPointMap();


                                object[] ob = new object[6];
                                ob[0] = getLatitude(tmp[3]);
                                ob[1] = getLongitude(tmp[2]);
                                ob[2] = sm.Description + " (" + sm.device_number + ")";
                                ob[3] = sm.lastDate;
                                ob[4] = sm.image;
                                ob[5] = true;
                                if (frmSatelliteMap.Visible == true)
                                    frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });

                                if (frmMapPoint.Visible == true)
                                {
                                    validCP100GPS("+"+getLatitude(tmp[3]), "+"+getLongitude(tmp[2]), sm.device_number, DateTime.Now.ToString(), true);
                                    //SetVehiclePosition(getLatitude(tmp[3]), getLongitude(tmp[2]), frmMapPoint.Map.ActiveMap, sm.Description, sm.Description + " (" + sm.device_number + ")", "", sm.image, false);
                                }

                                sm.Lat = getLatitude(tmp[3]);
                                sm.Lon = getLongitude(tmp[2]);
                                saveDevices(sm_array);
                                string Num = getNumberByDescription(sm.Description);

                                makeLog(Num, "Ricevuta posizione del terminale: " + Num);
                                Output("Ricevuta posizione del terminale: " + Num);
                                saveNewPosition(Num, "#-+" + tmp[3] + "-+" + tmp[2]);
                                Allarm(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(), Num, test);
                            }
                        }
                        else
                            Thread.Sleep(500);
                    }
                }
                catch { }
            }
        }


        public void loadDevices()
        {
            sm_array_temp = loadDevices(port);
            sm_array = new smartrack[sm_array_temp.Length];
            int cnt = 0;
            for (int i = 0; i < sm_array_temp.Length; i++)
            {
                if (sm_array_temp[i] != null)
                {
                    sm_array[cnt] = sm_array_temp[i];
                    cnt++;
                }

            }

            listBox1.Items.Clear();
            // checkedListBox1.Items.Clear();
            if (sm_array != null)
            {
                foreach (smartrack t in sm_array)
                {
                    if (t != null)
                    {
                        //listBox1.Items.Add(t.device_number);
                        listBox1.Items.Add(t.Description);
                    }
                }
            }
            else
                sm_array = new smartrack[int.MaxValue / 1024];
        }

        public void saveDevice(smartrack dev, int Index)
        {
            sm_array[Index] = dev;
            saveDevices(sm_array);
        }



        public void showDevice(string Number)
        {
            if(frmSatelliteMap.Visible)
                frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);

        //    if (frmMapPoint.Visible)
         //       clearMapPointMap();

            smartrack p = searchDevice(Number);
            object[] ob = new object[5];
            ob[0] = p.Lat;
            ob[1] = p.Lon;
            ob[2] = p.Description + " (" + p.device_number + ")";
            ob[3] = p.lastDate;
            ob[4] = p.image;

            if (frmSatelliteMap.Visible)
                frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
            if (frmMapPoint.Visible)
            {
                SetVehiclePosition(p.Lat, p.Lon, frmMapPoint.Map.ActiveMap, p.Description, p.Targa + " (" + p.device_number + ")", "", p.image, true);
                frmMapPoint.Map.ActiveMap.GoToLatLong(p.Lat, p.Lon, frmMapPoint.Map.ActiveMap.Altitude);
                if (Mode == 1)
                    showFlotta();
            }

        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        /*    if (log.Items.Count > 0)
            {
                if (log.Items[0].ToString().IndexOf("+CMT:") > -1)
                {
                    char t = '\"';
                    string[] gg = log.Items[0].ToString().Split(t);
                    lastNumber = gg[1];
                }
                if (log.Items[0].ToString().IndexOf("$GPRMC") > -1)
                    validGPS(log.Text);
                log.Items.RemoveAt(0);
            }*/

        }

        private void nuovoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new_device d = new new_device();
            DialogResult res = d.ShowDialog();
            if (res == DialogResult.OK)
            {
                int i = 0;
                for (i = 0; i < sm_array.Length; i++)
                    if (sm_array[i] == null) break;

                sm_array[i] = new smartrack(port, d.textBox1.Text);
                sm_array[i].image = d.textBox2.Text;
                sm_array[i].Description = d.textBox3.Text;
                saveDevices(sm_array);
                sm_array = loadDevices(port);

                listBox1.Items.Clear();
                // checkedListBox1.Items.Clear();
                if (sm_array != null)
                {
                    foreach (smartrack t in sm_array)
                    {
                        if (t != null)
                        {
                            //listBox1.Items.Add(t.device_number);
                            listBox1.Items.Add(t.Description);
                        }
                    }
                }
                else
                    sm_array = new smartrack[int.MaxValue / 1024];
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveDevices(sm_array);
            stopped = true;
            if (CommSetting.comm != null && CommSetting.comm.IsOpen())
                CommSetting.comm.Close();

            CommSetting.comm = null;
            if (portf != null)
            {
                portf.Close();
                portf.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                portf = null;
            }
            if (frmMapPoint.Visible)
            {
                if (frmMapPoint.Map.ActiveMap != null)
                {
                    frmMapPoint.Map.SaveMap();
                    frmMapPoint.Map.CloseMap();
                    frmMapPoint.Map.Dispose();
                }
            }
        }


        public int searchMatch(string Num, bool Selected)
        {
            int i = 0;
            bool find = false;
            for (i=0; i < listBox1.Items.Count; i++)
            {
                if (listBox1.Items[i].ToString().ToUpper().IndexOf(Num.ToUpper())>-1)
                {
                    if (Selected) listBox1.SelectedIndex = i;
                    find = true;
                    break;
                }
            }
            if (find)
                return i;
            else
                return -1;
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (e.KeyChar != '*')
                {
                    listBox1.SelectedIndex = -1;
                    if (searchMatch(txtSearch.Text, true) == -1)
                        MessageBox.Show("Dispositivo non trovato in memoria.");
                }
            }
            else if(e.KeyChar == 42)
            {
                    for(int i=0;i<listBox1.Items.Count;i++)
                        listBox1.SetItemChecked(i, true);                      
            }
        }

        private double getLatitude(string Lat)
        {
            string[] L = Lat.Split('.');
            if (L[0].Length > 1)
            {
                string ini = L[0].Substring(0, L[0].Length);  //2
                string other = L[0].Substring(L[0].Length, L[0].Length - L[0].Length) + "," + L[1];   //2

                double LAT = double.Parse(ini) + ((double.Parse(other) / 60));

                //LON : 071 + (6.288/60) = 071.1048

                string _lat = ini + "," + other + L[1];

                return LAT;
            }
            return 0;
        }

        private double getLongitude(string Lat)
        {
            string[] L = Lat.Split('.');
            if (L[0].Length > 1)
            {
                string ini = L[0].Substring(0, L[0].Length);   //3
                string other = L[0].Substring(L[0].Length, L[0].Length - L[0].Length) + "," + L[1];   //3

                double LON = double.Parse(ini) + ((double.Parse(other) / 60));

                //LON : 071 + (6.288/60) = 071.1048

                return LON;
            }
            return 0;
        }

        public void validGPS(string ret,string number,string TimeDate,bool Clear)
        {
            if (!string.IsNullOrEmpty(ret))
            {
                if(Clear)
                    frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);

                string[] Coord = ret.Split(',');
                string _latitude = Coord[3];
                string _longitude = Coord[5];
                double KmH;
                try
                {
                    string _speed = Coord[7];
                    KmH = 0.00;
                    if (!string.IsNullOrEmpty(_speed))
                    {
                        _speed = _speed.Replace('.', ',');
                        KmH = Double.Parse(_speed) * 1.852;
                    }
                }
                catch { KmH = 0.00; }
                double lat = getLatitude(_latitude);
                double lon = getLongitude(_longitude);

                smartrack pp = searchDevice(number);

                if (frmSatelliteMap.Visible == true)
                {
                    object[] ob = new object[5];
                    ob[0] = lat;// TrasformLatitude(_latitude);
                    ob[1] = lon;// TrasformLongitude(_longitude);
                    ob[2] = pp.Description + " (" + number + ")";
                    ob[3] = TimeDate;
                    ob[4] = pp.image;
                    frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
                }
                if (frmMapPoint.Visible == true)
                {
                    SetVehiclePosition(lat, lon, frmMapPoint.Map.ActiveMap, pp.Description, pp.Targa + " (" + number + ")", "", pp.image,Clear);
                    //*************************************************************
                    //*** CONTROLLARE SE IL MEZZO HA ASSOCIATO UN'OBIETTIVO 
                    //*** E IN TAL CASO CONTROLLARE SE E' ALL'INTERNO
                    //*************************************************************
                    if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        Location bers = null;
                        string[] hh = pp.Bersaglio.Split('|');
                        if (!string.IsNullOrEmpty(hh[1]) && !string.IsNullOrEmpty(hh[2]))
                            bers = frmMapPoint.Map.ActiveMap.GetLocation(double.Parse(hh[1]), double.Parse(hh[2]), frmMapPoint.Map.ActiveMap.Altitude);
                        //else
                        //   bers = frmMapPoint.Map.ActiveMap.

                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, bers.Latitude, bers.Longitude, lat, lon);
                    }

                    /*if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, pp.Bersaglio.Latitude, pp.Bersaglio.Longitude, lat, lon);
                    }*/
                }

                //smartrack pp = searchDevice(number);
                pp.Lat = lat;
                pp.Lon = lon;
                pp.lastDate = TimeDate;

                saveDevices(sm_array);
              }
        }

        public smartrack searchDevice(string TelNumber)
        {
            foreach(smartrack p in sm_array)
            {
                if (p != null)
                {
                    if (p.device_number == TelNumber)
                        return p;
                }
                else
                    return null;
            }
            return null;
        }

        public smartrack searchDevicebyDescription(string TelNumber)
        {
            foreach (smartrack p in sm_array)
            {
                if (p != null)
                {
                    if (p.Description == TelNumber)
                        return p;
                }
              //  else
                 //   return null;
            }
            return null;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            checkSMS();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            
        }

        private void searchStorico()
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                if (listBox1.SelectedItems.Count == 1)
                {
                    dataGridView1.Rows.Clear();
                    updateConnectionString();

                    SqlConnection myConnection = new SqlConnection(connectionString);
                    myConnection.Open();
                    string Upd = "UPDATE Position SET DateTime = REPLACE(DateTime,'.',':');";
                    SqlCommand cmd = new SqlCommand(Upd, myConnection);
                    cmd.ExecuteNonQuery();
                    myConnection.Close();

                    smartrack num = searchDevicebyDescription(listBox1.SelectedItems[0].ToString());
                    string data;
                    string data2;
                    //data = dateFrom.Value.Month + "/" + dateFrom.Value.Day + "/" + dateFrom.Value.Year;
                    //data2 = dateTo.Value.Month + "/" + dateTo.Value.Day + "/" + dateTo.Value.Year;
                    data = dateFrom.Value.Month + "/" + dateFrom.Value.Day + "/" + dateFrom.Value.Year;
                    data2 = dateTo.Value.Month + "/" + dateTo.Value.Day + "/" + dateTo.Value.Year;
                    string SQL = "SELECT * FROM Position WHERE (CONVERT(DATETIME, DateTime, 103) > '" + data + "') AND (CONVERT(DATETIME, DateTime, 103) < '" + data2 + "') AND Vehicle='" + num.device_number  + "'";

                    SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
                    System.Data.DataSet ds = new System.Data.DataSet();
                    da.Fill(ds, "pos");

                    if (ds.Tables["pos"].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables["pos"].Rows.Count; i++)
                        {
                            string position = ds.Tables["pos"].Rows[i]["Position"].ToString();
                            string date_time = ds.Tables["pos"].Rows[i]["DateTime"].ToString();
                            dataGridView1.Rows.Add();
                            dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = position;
                            dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[1].Value = date_time;
                        }
                    }
                }
               /* else
                {
                    MessageBox.Show(this, "Per visualizzare lo storico è necessario selezionare un solo mezzo.", "Storico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }*/
            }
        }

        public string returnNMEACoord(string ret)
        {
                string[] Coord = ret.Split(',');
                string _latitude = Coord[3];
                string _longitude = Coord[5];

                double lat = getLatitude(_latitude);
                double lon = getLongitude(_longitude);

                return lat.ToString() + "§" + lon + ToString();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count >= 2)
            {
                double TotalKM = 0;
                for(int i=0; i < dataGridView1.SelectedRows.Count;i++)
                {
                    if (i + 1 <= dataGridView1.SelectedRows.Count - 1)
                    {
                        string point1 = dataGridView1[0, i].Value.ToString();
                        string point2 = dataGridView1[0, i + 1].Value.ToString();

                        if (point1.IndexOf("$GPRMC") > -1 && point2.IndexOf("$GPRMC") > -1)
                        {
                            string[] p1 = returnNMEACoord(point1).Split('§');
                            string[] p2 = returnNMEACoord(point2).Split('§');

                            TotalKM += distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
                        }

                        if ((point1.IndexOf("KTA") > -1 && point2.IndexOf("KTA") > -1) || (point1.IndexOf("POS") > -1 && point2.IndexOf("POs") > -1))
                        {
                            string[] p1 = returnKFTCoord(point1).Split('§');
                            string[] p2 = returnKFTCoord(point2).Split('§');

                            TotalKM += distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
                        }

                        if (point1.IndexOf("\nHP=") > -1 && point2.IndexOf("\nHP=") > -1)
                        {
                            string[] p1 = returnCP100Coord(point1).Split('§');
                            string[] p2 = returnCP100Coord(point2).Split('§');

                            if(p1.Length>1 && p2.Length>1)
                                TotalKM += distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
                        }

                        
                    }
                }
                TotalKM = Math.Round(TotalKM, 3);
                if(TotalKM == 0.039) TotalKM=0.0;

                grpStorico.Text = "Storico: Km totali (stimati): " + TotalKM.ToString();
            }
        }


        //:::  Passed to function:                                                    :::
        //:::    lat1, lon1 = Latitude and Longitude of point 1 (in decimal degrees)  :::
        //:::    lat2, lon2 = Latitude and Longitude of point 2 (in decimal degrees)  :::
        //:::    unit = the unit you desire for results                               :::
        //:::           where: 'M' is statute miles                                   :::
        //:::                  'K' is kilometers (default)                            :::
        //:::                  'N' is nautical miles                                  :::
        public double distance(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return (dist);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal degrees to radians             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            {
                case 0: deviceCtrl1.Visible = true;
                        grpVeicoli.Visible = false;
                        grpStorico.Visible = false;
                        Mode = 0; 
                        tmrFlotta.Enabled = false;
                        if (FLOTTA != null){
                            FLOTTA.Delete();
                        }
                        FLOTTA = null;
                        break;
                case 1: deviceCtrl1.Visible = false;
                        grpVeicoli.Visible = true;
                        grpStorico.Visible = false;
                        Mode = 0;
                        //showFlotta();
                        //tmrFlotta.Enabled = true;
                        break;
                case 2: deviceCtrl1.Visible = false;
                        grpVeicoli.Visible = false;
                        grpStorico.Visible = true;
                        Mode = 2;
                        dateFrom.Value = DateTime.Now.Subtract(TimeSpan.FromDays(1));
                        dateTo.Value = DateTime.Now;
                        tmrFlotta.Enabled = false;
                        grpStorico.Text = "Storico";
                        FLOTTA = null;
                        if (FLOTTA != null){
                            FLOTTA.Delete();
                        }
                        searchStorico();
                        break;
            }
        }

        private void showFlotta()
        {
            if (Mode <= 1)
            {
                numbers.Clear();
                foreach (string num in listBox1.CheckedItems)
                {
                    numbers.Add(num);
                }
                getPosition(numbers);
            }
            if (Mode == 2)
                searchStorico();
        }

        private void listBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            listBox1.Update();
            if(Mode==1)
                showFlotta();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                frmDestination frm = new frmDestination();
                frm.mainFrm = this;
                frm.ShowDialog();
           }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                frmObiettivo frm = new frmObiettivo();
                frm.mainFrm = this;
                frm.Show();
                /*if (DialogResult.OK == frm.ShowDialog())
                {
                    string[] coord = frm.txtDestination.Text.Split('-');

                    smartrack p = searchDevicebyDescription(listBox1.SelectedItem.ToString());
                    //smartrack p = sm_array[listBox1.SelectedIndex];

                    object[] ob = new object[4];
                    ob = new object[4];
                    ob[0] = p.Lat;
                    ob[1] = p.Lon;
                    ob[2] = coord[1];
                    ob[3] = coord[2];
                    frmSatelliteMap.satelliteMap.Invoke(new getDistanceToPointDelegate(getDistanceToPoint), new object[] { ob });
                }*/
            }
        }

        private void mappaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*if (frmSatelliteMap.Visible == false)
            {
                frmSatelliteMap = new frmMap(); frmSatelliteMap.Show(); 
                frmSatelliteMap.satelliteMap.Navigate(@AppDomain.CurrentDomain.BaseDirectory + "map.htm");
                frmSatelliteMap.satelliteMap.Document.InvokeScript("SetMapStyle", new object[] { "VEMapStyle.Hybrid" });
            }*/
        }

        private void notificheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmAllarm.Visible == false)
            { 
                frmAllarm = new frmAllarm();
                frmAllarm.mainFrm = this;
                frmAllarm.Show();
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                string position = dataGridView1[0, e.RowIndex].Value.ToString();
                string data_ora = dataGridView1[0, e.RowIndex].Value.ToString();
                smartrack device = searchDevicebyDescription(listBox1.SelectedItems[0].ToString());

                if (position.IndexOf("$GPRMC") > -1)
                    validGPS(position, device.device_number, data_ora, true);
                if (position.IndexOf("KTA") > -1 || position.IndexOf("POS") > -1)
                    parseKFTString(position, device.device_number, data_ora, true);
                if (position.IndexOf("HP") > -1)
                    parseCP100stringLoc(position, device.device_number, data_ora, true);
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            button1_Click_2(null, null);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            showFlotta();
            tmrFlotta.Enabled = false;
        }

        private void opzioniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            options opt = new options();
            opt.mainFrm = this;
            loadOptions();
            opt.textBox1.Text = COM_PORT;
            opt.textBox2.Text = COM_SPEED;
            opt.textBox3.Text = COM_TIMEOUT;
            opt.textBox4.Text = SMS_C;
            opt.txtRadius.Text = RADIUS_BERSAGLIO;
            opt.txtSosta.Text = TEMPO_SOSTA;
            opt.ShowDialog();
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            dataGridView1.SelectAll();
            button1_Click_2(null, null);
        }

        private void deviceCtrl1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            
        }

        private void chiamaSMSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                smartrack device = searchDevicebyDescription(listBox1.SelectedItem.ToString());
                if (device != null)
                {
                    device.requestPosition();
                    setFullBar(true);
                }
            }
        }

        private void eliminaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string curr_dev = listBox1.SelectedItem.ToString();

            if (DialogResult.Yes == MessageBox.Show(this, "Sicuro di voler eliminare il dispositivo " + curr_dev + "?", "Avviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                int i = 0;
                foreach (smartrack sm in sm_array)
                {
                    if ((sm!=null) && (sm.Description == curr_dev))
                    {
                        sm_array[i] = null;
                        break;
                    }
                    i++;
                }
                saveDevices(sm_array);
                loadDevices();
            }
        }

        private void webToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmMapPoint != null)
                if (frmMapPoint.Visible == true)
                    frmMapPoint.Visible = false;

            if (frmSatelliteMap != null)
            {
                if( frmSatelliteMap.Visible == false)
                    frmSatelliteMap.Visible = true;
            }
            else
            {
                frmSatelliteMap = new frmMap();
                frmSatelliteMap.Show();
            }

        }

        private void classicaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmSatelliteMap != null)
                if (frmSatelliteMap.Visible == true)
                    frmSatelliteMap.Visible = false;

            if (frmMapPoint != null)
            {
                try
                {
                    if (frmMapPoint.Visible == false)
                    {
                        frmMapPoint.Visible = true;
                        frmMapPoint.loadMap();
                    }
                }
                catch
                {
                    frmMapPoint = new frmMapPoint();
                    frmMapPoint.mainFrm = this;
                    frmMapPoint.Show();
                    frmMapPoint.loadMap();
                }
            }
        }

        private void button2_Click_3(object sender, EventArgs e)
        {
            bool Itinerario = false;

            DialogResult res = MessageBox.Show(this, "Visualizzare anche il percorso?", "Track", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
                Itinerario = true;

            MapPoint.Map map = frmMapPoint.Map.ActiveMap;
            map.ActiveRoute.Clear();

            MapPoint.Location point;
            smartrack device = searchDevicebyDescription(listBox1.SelectedItems[0].ToString());
            for (int i = 0; i < dataGridView1.SelectedRows.Count; i++)
            {
                string position = dataGridView1[0, i].Value.ToString();
                string data_ora = dataGridView1[0, i].Value.ToString();
                
                string P = "";
                if (position.IndexOf("$GPRMC") > -1)
                    P = returnNMEACoord(position);
                if (position.IndexOf("KTA") > -1)
                    P = returnKFTCoord(position);
                if (position.IndexOf("\nHP=") > -1)
                    P = returnCP100Coord(position);
                string[] pp = P.Split('§');

                if (pp.Length > 1)
                {
                    point = map.GetLocation(double.Parse(pp[0]), double.Parse(pp[1]), map.Altitude);
                    map.ActiveRoute.Waypoints.Add(point, device.Description+"["+device.Targa+"] Punto" + i.ToString());
                    map.AddPushpin(point, i.ToString());
                }
            }

            if (Itinerario)
            {
                map.ActiveRoute.Application.ItineraryVisible = false;
                try
                {
                    map.ActiveRoute.Calculate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Impossibile calcolare il percorso. Motivo:" + ex.Message);
                }
            }
        }

        private void xpLoginEntry1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            
        }

        private void opzioniToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            
        }

        private void profiliDiGuidaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmDriver frm = new frmDriver();
            frm.device = frmMapPoint;
            frm.mainFrm = this;
            frm.Show();
        }

        private void xpLoginEntry2_Load(object sender, EventArgs e)
        {

        }
        

        private void bersagliToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frm_obiettivo = new frmObiettivo();
            frm_obiettivo.mainFrm = this;
            frm_obiettivo.olnyAdd = true;
            frm_obiettivo.Show();
        }

        private void kmPercorsiannoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                frmKmAnno frm = new frmKmAnno();
                frm.mainFrm = this;
                smartrack num = searchDevicebyDescription(listBox1.SelectedItems[0].ToString());
                frm.label1.Text = "Mezzo selezionato: " + num.Description + " [" + num.Targa + "]";
                frm.Show();
            }
            else
                MessageBox.Show("Per visualizzare i report è necessario selezionare un mezzo.");
        }

        private void selezionaServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void selezionaBasiDiDatiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSQLServer frm = new frmSQLServer();
            frm.mainFrm = this;
            frm.Show();
        }

        private void utentiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmUser frm = new frmUser();
            frm.mainFrm = this;
            frm.Show();
        }

        private void sosteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSoste frm = new frmSoste();
            frm.mainFrm = this;
            frm.Show();
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            searchStorico();
        }

        private void modoFlottaToolStripMenuItem_Click(object sender, EventArgs e)
        {
                modoFlottaToolStripMenuItem.Checked = !modoFlottaToolStripMenuItem.Checked;
                if (modoFlottaToolStripMenuItem.Checked)
                {
                    oldMode = Mode;
                    Mode = 1;
                    showFlotta();
                    tmrFlotta.Enabled = true;
              
                }
                else
                {
                    Mode = oldMode;
                    tmrFlotta.Enabled = false;
                }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void listBox1_Click(object sender, EventArgs e)
        {
            if (Mode == 1)
            {
                deviceCtrl1.DeviceINDEX = listBox1.SelectedIndex;
                deviceCtrl1.mainFrm = this;
                smartrack sm = searchDevicebyDescription(listBox1.SelectedItem.ToString());
                deviceCtrl1.setDevice(sm);
            }
            if (Mode == 2)
                searchStorico();
            if (Mode == 0)
            {
                if (listBox1.SelectedIndex > -1)// && tabControl1.SelectedIndex == 0)
                {
                    deviceCtrl1.DeviceINDEX = listBox1.SelectedIndex;
                    deviceCtrl1.mainFrm = this;
                    smartrack sm = searchDevicebyDescription(listBox1.SelectedItem.ToString());
                    deviceCtrl1.setDevice(sm);
                    //log.Clear();

                    if (frmSatelliteMap.Visible == true)
                        frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);
                    if (frmMapPoint.Visible == true)
                        clearMapPointMap();

                    smartrack p = sm_array[listBox1.SelectedIndex];

                    if (frmSatelliteMap.Visible == true)
                    {
                        object[] ob = new object[6];
                        ob[0] = p.Lat;
                        ob[1] = p.Lon;
                        ob[2] = p.Description + " (" + p.device_number + ")";
                        ob[3] = p.lastDate;
                        ob[4] = p.image;
                        ob[5] = true;
                        if (frmSatelliteMap.Visible == true)
                            frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
                    }
                    if (frmMapPoint.Visible == true)
                    {
                        if(p!=null)
                            SetVehiclePosition(p.Lat, p.Lon, frmMapPoint.Map.ActiveMap, p.Description, p.Targa + " (" + p.device_number + ")", "", p.image, false);
                    }
                }
            }
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {

        }

        private void cancellaLOGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            log.Clear();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void importaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Per l'importazione del file csv ricordarsi di inserire:\r\n1° Riga: Password di default\r\n2° Riga: Modello predefinito di periferica\r\n3° Riga: Path dell'immagine di default.\r\nRestanti righe: Nome;ID;Targa;NTelefonico.");
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fname = openFileDialog1.FileName;

                for (int i = 0; i < sm_array.Length; i++)
                {
                    if(sm_array[i] != null)
                        sm_array[i] = null;
                }
                saveDevices(sm_array);
                loadDevices();

                using (StreamReader rd = new StreamReader(fname))
                {
                    int i = 0;
                    string passwd = rd.ReadLine();
                    string type = rd.ReadLine();
                    string default_image = rd.ReadLine();
                    while (!rd.EndOfStream)
                    {
                        string[] curr = rd.ReadLine().Split(';');
                        if (sm_array[i] == null)
                            sm_array[i] = new smartrack(port, "");
                        sm_array[i].Description = curr[0]+"/"+curr[1];
                        sm_array[i].device_number = "+39" + curr[3].Replace("/", "");
                        sm_array[i].Targa = curr[2].Replace(" ", "");
                        sm_array[i].devicePassword = passwd;
                        sm_array[i].DeviceType = type;
                        sm_array[i].Bersaglio = null;
                        sm_array[i].image = default_image;
                        i++;
                    }
                }
                saveDevices(sm_array);
                loadDevices();
            }
        }

        private void esportaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fname = saveFileDialog1.FileName;

                using (StreamWriter wr = new StreamWriter(fname))
                {
                    for (int i = 0; i < sm_array.Length; i++)
                    {
                        if (sm_array[i] != null)
                        {
                            string _out="";
                            string[] hh = sm_array[i].Description.Split('/');
                            _out = hh[0]+";"+hh[1]+";"+sm_array[i].Targa+";"+sm_array[i].device_number;
                            wr.WriteLine(_out);
                        }
                        else break;
                    }
                }
                MessageBox.Show("Esportazione completata!");
            }
        }

        private void chiamaDATIToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void listenClient()
        {
            Thread.CurrentThread.Name = "TCP Thread";
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 81);
            try
            {
                tcpListener.Start();
                while (this.Visible == true)
                {
                    if (tcpListener.Pending())
                    {
                        Socket soTcp = tcpListener.AcceptSocket();
                        ThreadPool.QueueUserWorkItem(new WaitCallback(acceptClientConnection), soTcp);
                        tcpListener.Stop();
                        tcpListener.Start();
                    }
                    else
                        Thread.Sleep(500);
                }
                tcpListener.Stop();
            }
            catch (SocketException se)
            {
                Console.WriteLine("A Socket Exception has occurred!" + se.ToString());
            }
        }
        /*
         * Accetta una nuova connessione via TCP e legge i dati del nuovo Host
         */
        public void acceptClientConnection(Object sock)
        {
            acceptClientConnection((Socket)sock);
        }

        public void acceptClientConnection(Socket oldsock)
        {
            int TimeOut = 0;
            string sEndPoint = "";
            //CLIENT_COUNT++;
            Socket sock = new Socket(oldsock.DuplicateAndClose(Process.GetCurrentProcess().Id));
            sEndPoint = sock.RemoteEndPoint.ToString();
            sock.ReceiveTimeout = 25000;
            NetworkStream st = new NetworkStream(sock);
            StreamReader rd = null;
            bool Connected = true;
            bool valid = true;
            int base_cnt = 0;
            int keep_alive = 0;
            string current="";
            int y = 0;
            string test = "";
            while (sock.Connected)
            {
                if (st.DataAvailable)
                {
                    rd = new StreamReader(st);
                    test = rd.ReadLine();

                    if (test.IndexOf("MESSAGE") > -1)
                    {
                        string[] arr = test.Split('|');
                        Allarm(DateTime.Now.ToString(), arr[0], arr[2]);
                        current = arr[0];
                        if (!Connessioni.ContainsKey(arr[0]))
                            Connessioni.Add(arr[0], st);
                    }
                    if (test.IndexOf("GPS") > -1)
                    {
                        string[] arr = test.Split('|');
                        validGPSTablet(arr[2] + "," + arr[3] + "," + arr[4] + "," + arr[5], arr[0], DateTime.Now.ToString(), true);
                        current = arr[0];
                        if (!Connessioni.ContainsKey(arr[0]))
                            Connessioni.Add(arr[0], st);
                    }
                    if (test.IndexOf("ALLARME") > -1)
                    {
                        string[] arr = test.Split('|');
                        Allarm(DateTime.Now.ToString(), arr[0], arr[2]);
                        current = arr[0];
                        if (!Connessioni.ContainsKey(arr[0]))
                            Connessioni.Add(arr[0], st);
                    }
                    if (test.IndexOf("READY") > -1)
                    {
                        keep_alive--;
                        string[] arr = test.Split('|');
                        current = arr[0];
                        if (!Connessioni.ContainsKey(arr[0]))
                            Connessioni.Add(arr[0], st);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                    base_cnt++;
                    if (base_cnt >= 100)
                    {
                        base_cnt = 0;
                        StreamWriter wr = new StreamWriter(st);
                        wr.Write("KEEP ALIVE");
                        wr.Flush();
                        keep_alive++;
                        if (keep_alive >= 5)
                        {
                            Output("L'unità: " + current + " non risponde.");
                        }
                    }
                }
            }
         //  Alla Disconnessione
            Connessioni.Remove(current);
            Output(current + " si è disconnesso.");
        }

        public void validGPSTablet(string ret, string UNIT, string TimeDate, bool Clear)
        {
            if (!string.IsNullOrEmpty(ret))
            {
                if (Clear)
                    frmSatelliteMap.satelliteMap.Invoke(new ClearMapDelegate(ClearMap), null);

                string[] Coord = ret.Split(',');
                string _latitude = Coord[0];
                string _longitude = Coord[1];
                string KmH = Coord[2];
                double lat = Double.Parse(_latitude.Replace('.',','));// getLatitude(_latitude);
                double lon = Double.Parse(_longitude.Replace('.', ','));// getLongitude(_longitude);

                smartrack pp = searchDevice(UNIT);

                if (frmSatelliteMap.Visible == true)
                {
                    object[] ob = new object[5];
                    ob[0] = lat;// TrasformLatitude(_latitude);
                    ob[1] = lon;// TrasformLongitude(_longitude);
                    ob[2] = pp.Description + " (" + UNIT + ")";
                    ob[3] = TimeDate;
                    ob[4] = pp.image;
                    frmSatelliteMap.satelliteMap.Invoke(new SetPushpinDelegate(SetPushpin), new object[] { ob });
                }
                if (frmMapPoint.Visible == true)
                {
                    SetVehiclePosition(lat, lon, frmMapPoint.Map.ActiveMap, pp.Description, pp.Targa + " (" + UNIT + ")", "", pp.image, Clear);
                    //*************************************************************
                    //*** CONTROLLARE SE IL MEZZO HA ASSOCIATO UN'OBIETTIVO 
                    //*** E IN TAL CASO CONTROLLARE SE E' ALL'INTERNO
                    //*************************************************************
                    if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        Location bers = null;
                        string[] hh = pp.Bersaglio.Split('|');
                        if (!string.IsNullOrEmpty(hh[1]) && !string.IsNullOrEmpty(hh[2]))
                            bers = frmMapPoint.Map.ActiveMap.GetLocation(double.Parse(hh[1]), double.Parse(hh[2]), frmMapPoint.Map.ActiveMap.Altitude);
                        //else
                        //   bers = frmMapPoint.Map.ActiveMap.

                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, bers.Latitude, bers.Longitude, lat, lon);
                    }

                    /*if (pp.Bersaglio != null)               //esiste un bersaglio
                    {
                        MapUtils mu = new MapUtils(pp, this);
                        mu.radiusSearch(frmMapPoint.Map.ActiveMap, pp.Bersaglio.Latitude, pp.Bersaglio.Longitude, lat, lon);
                    }*/
                }

                //smartrack pp = searchDevice(number);
                pp.Lat = lat;
                pp.Lon = lon;
                pp.lastDate = TimeDate;

                saveDevices(sm_array);
            }
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedItem!=null)
            {
                string unit = listBox1.SelectedItem.ToString();
                smartrack sm = searchDevicebyDescription(unit);
                if (sm!=null)
                {
                    if (Connessioni.ContainsKey(sm.device_number))
                    {
                        frmMessaggi mess = new frmMessaggi();
                        mess.init(Connessioni[sm.device_number]);
                        mess.Show();
                    }
                    else
                        MessageBox.Show("Impossibile inviare messaggi all'unità " + unit + " perchè non ha mai trasmesso.");
                }
            }
        }
    }
}

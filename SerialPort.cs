using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Windows.Forms;

using GsmComm.PduConverter;
using GsmComm.GsmCommunication;

namespace SMS
{
    [Serializable()]
    public class SerialCOMPort
    {
        //SerialPort com;
        public Form1 mainFrm;
        public SerialCOMPort()
        {
     
        }

        private void SMSThread(object cmd)
        {
            string[] param = (string[])cmd;
            SMSThread(param);
        }

        private void SMSThread(string[] buffer)
        {
            try
            {
               /* SmsSubmitPdu pdu;
                pdu = new SmsSubmitPdu(buffer[0], buffer[1], "");  // "" indicate SMSC No
                CommSetting.comm.SendMessage(pdu);
                */
                /*if (!mainFrm.sendMsg(buffer[1], buffer[0]))
                {
                    int tryout = 0;
                    mainFrm.Output("Invio SMS non riuscito. Ritento invio...");
                    while (mainFrm.sendMsg(buffer[1], buffer[0]) == false)
                    {
                        string command = "" + char.ConvertFromUtf32(26) + "\r";
                        mainFrm.ExecCommand(command, 3000, "Failed to send message"); //3 seconds
                        Thread.Sleep(2000);
                        tryout++;
                        if (tryout >= 4) break;
                    }
                    
                }*/
                mainFrm.sendMsg(buffer[1], buffer[0]);
                if (mainFrm != null)
                    mainFrm.Output("Inviato sms a: " + buffer[1]);
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SendSMS(string buffer,string tel_number)
        {
            string[] param = new string[2];
            param[0] = buffer;
            param[1] = tel_number;
            ThreadPool.QueueUserWorkItem(new WaitCallback(SMSThread),param); 
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElzeKool.Devices;
using MapPoint;

namespace SMS
{
    [Serializable()]
    public class smartrack
    {
        public string devicePassword = "1234";
        public string device_number;
        public string currentServer;
        public string currentPort;
        public string APN;
        public bool GPRSMode;
        public bool SMSMode;
        public string AlarmMode;
        public bool AllarmeMovimento;
        public string Raggio;
        public bool AllarmeVelocita;
        public string Velocita;
        public bool Energia;
        public bool Carburante;
        public string ResponseFormat;
        public bool PollingMode;
        public string Intervallo;
        public string Volte;
        public bool Started;
        public double Lat;
        public double Lon;
        public string lastDate;
        public string image;
        public string Description;
        public string Targa;
        public string DeviceType;
        public string Bersaglio;

        [NonSerialized()]
        public SerialCOMPort serialPort;

        public smartrack(SerialCOMPort port,string device_number)
        {
            this.serialPort = port;
            this.device_number = device_number;
            this.Bersaglio = null;
        }

        public void SetBersaglio(string loc)
        {
            this.Bersaglio = loc;
        }

        public void setDeviceType(string Type)
        {
            this.DeviceType = Type;
        }

        public void setServer(string server, string port)
        {
             serialPort.SendSMS("ip " + server + " port" + port, device_number);
            this.currentServer = server;
            this.currentPort = port;
        }

        public void setAPN(string APN)
        {
            serialPort.SendSMS("apn" + devicePassword + " " + APN, device_number);
            this.APN = APN;
        }

        public void useGPRS()
        {
            serialPort.SendSMS("web" + devicePassword, device_number);
            GPRSMode = true;
            SMSMode = false;
        }

        public void useSMS()
        {
            serialPort.SendSMS("telephone" + devicePassword, device_number);
            GPRSMode = false;
            SMSMode = true;
        }

        public void setAlarm(string on_off)
        {
            serialPort.SendSMS("alert" + devicePassword + " " + on_off, device_number);
            this.AlarmMode = on_off;
        }

        public void disabilitaAllarmeMovimento()
        {
            serialPort.SendSMS("nomove" + devicePassword, device_number);
            this.AllarmeMovimento = false;
        }

        public void allarmeMovimento(string raggio)
        {
            serialPort.SendSMS("move" + devicePassword + " " + raggio, device_number);
            AllarmeMovimento = true;
            this.Raggio = raggio;
        }

        public void disabilitaVelocita()
        {
            serialPort.SendSMS("nospeed" + devicePassword, device_number);
            this.AllarmeVelocita = false;
        }

        public void allarmeVelocita(string velocita)
        {
            serialPort.SendSMS("speed" + devicePassword + " " + velocita, device_number);
            this.AllarmeVelocita = true;
            this.Velocita = velocita;
        }

        public void attivaEnergia()
        {
            serialPort.SendSMS("supplyelec" + devicePassword, device_number);
            this.Energia = true;
        }

        public void bloccaEnergia()
        {
            serialPort.SendSMS("stopelec" + devicePassword, device_number);
            this.Energia = false;
        }

        public void attivaCarburante()
        {
            serialPort.SendSMS("supplyoil" + devicePassword, device_number);
            this.Carburante = true;
        }

        public void bloccaCarburante()
        {
            serialPort.SendSMS("stopoil" + devicePassword, device_number);
            this.Carburante = false;
        }

        public void setResponseFormat(string Format)
        {
            serialPort.SendSMS("format" + devicePassword + " " + Format, device_number);
            this.ResponseFormat = Format;
        }

        public void Logout()
        {
            if(this.DeviceType == "KFT")
                serialPort.SendSMS("KTA" + devicePassword+"LOGOUT", device_number);
        }

        public void stopPolling()
        {
            serialPort.SendSMS("noat" + devicePassword, device_number);
            this.PollingMode = false;
        }

        public void setPolling(string intervallo)
        {
            if (this.DeviceType == "CP-100")
            {
                if (string.IsNullOrEmpty(intervallo) || intervallo == "0")
                {
                    serialPort.SendSMS("P N", device_number);
                    this.PollingMode = false;
                    this.Intervallo = "0";
                }
                else
                {
                    serialPort.SendSMS("P Y " + intervallo, device_number);
                    this.PollingMode = true;
                    this.Intervallo = intervallo;
                }
            }
        }

        public void disableAdministrator(string number)
        {
            if (number.IndexOf("+39") == -1) number = "+39" + number;
            serialPort.SendSMS("noadmin" + devicePassword + " " + number, device_number);
        }

        public void setNewAdministrator(string number)
        {
            if (number.IndexOf("+39") == -1) number = "+39" + number;
            serialPort.SendSMS("admin" + devicePassword + " " + number, device_number);
        }

        public void setNewPassword(string newPassword)
        {
            if(this.DeviceType == "CP-100")
                serialPort.SendSMS("R 1 " + devicePassword + " " + newPassword, device_number);
            if (this.DeviceType == "KFT")
                serialPort.SendSMS("KTA" + devicePassword + "PWD" + newPassword, device_number);
            if (this.DeviceType == "MT-800")
                serialPort.SendSMS("*ID,"+devicePassword+",setpass,new password,new password#",device_number);
            this.devicePassword = newPassword;
        }

        public void requestPosition()
        {
            if (!device_number.Contains("UNITA"))
            {
                if (this.DeviceType == "CP-100")
                    serialPort.SendSMS("C", device_number);
                if (this.DeviceType == "KFT")
                    serialPort.SendSMS("KTA" + devicePassword + "POS\r\n", device_number);   //\r\n
            }
        }

        public void initDevice()
        {
            if (this.DeviceType == "KFT")
            {
                serialPort.SendSMS("KTA" + devicePassword + "LOGIN", device_number);
                Started = true;
            }
        }

        public void settaUscitaON(string uscita)
        {
            if(this.DeviceType=="KFT")
                serialPort.SendSMS("KTA" + devicePassword + "S"+uscita+"\r\n", device_number);
        }

        public void settaUscitaOFF(string uscita)
        {
            if(this.DeviceType == "KFT")
                serialPort.SendSMS("KTA" + devicePassword + "C" + uscita + "\r\n", device_number);
        }
    }
}

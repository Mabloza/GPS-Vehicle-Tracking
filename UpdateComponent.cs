using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace SMS
{
    class UpdateComponent
    {
        public void checkSite()
        {
            string RemoteDomain = "http://www.2858.it";
            try
            {  
                IPHostEntry inetServer = Dns.GetHostEntry(RemoteDomain.Replace("http://", String.Empty)); 
            } 
            catch 
            {
            //    DialogResult result = MessageBox.Show(String.Format("Impossibile connettersi a {0}.\nPrego controllare la connessione e riprovare.", RemoteDomain), "Errore nel controllo aggiornamento", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            } 

            try
            {
                string versionFileUrl = "http://www.2858.it/track/track.update.txt";   
                System.Net.WebClient client = new System.Net.WebClient();
                string yy = String.Format("{0}\\{1}", System.Environment.ExpandEnvironmentVariables("%TEMP%"), "test.txt");
                client.DownloadFile(versionFileUrl,yy); 
            } 
            catch (Exception ex)
            {
                string errorDetails = ex.Message; 
                //MessageBoxIcon iconsToShow = MessageBoxIcon.Information; 
                if (ex.Message.Contains("could not be resolved")) 
                { 
                    errorDetails = String.Format("Errore nel connettersi a {0}.\nPrego controllare la connessione e riprovare.", RemoteDomain); 
                    //iconsToShow = MessageBoxIcon.Error; 
                } 
                else 
                    if (ex.Message.Contains("404")) 
                    { 
                        errorDetails = "Il server per l'aggiornamento non è al momento disponibile.\nPlease try again later."; 
                    //    iconsToShow = MessageBoxIcon.Information; 
                    } 
                //DialogResult result = MessageBox.Show(String.Format("{0}", errorDetails), "Error downloading file", MessageBoxButtons.OK, iconsToShow); 
                return; 
            }

            string versionFileLocal = "";
            string shortVersionFromFile = "";
            string longVersionFromFile = "";
            string longVersionFromVrs = "";
            string shortVersionFromVrs = "";

           // if (File.Exists(config.VersionFileLocal)) 
           // {   
                versionFileLocal = String.Format("{0}\\{1}", System.Environment.ExpandEnvironmentVariables("%TEMP%"), "test.txt");   
                TextReader tr = new StreamReader(versionFileLocal);   string tempStr = tr.ReadLine();   
                tr.Close();   
                File.Delete(versionFileLocal);   
                longVersionFromFile = tempStr;   
                shortVersionFromFile = tempStr.Replace(".", String.Empty);   
                Version vrs = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;   
                longVersionFromVrs = String.Format("{0}.{1}.{2}.{3}", vrs.Major, vrs.Minor, vrs.MajorRevision, vrs.MinorRevision);   
                shortVersionFromVrs = String.Format("{0}{1}{2}{3}", vrs.Major, vrs.Minor, vrs.MajorRevision,   vrs.MinorRevision);  
        /*    } 
            else
            {   
                DialogResult result = MessageBox.Show(String.Format("Version file not found or not accessible.\nPlease check that you have permission to read the {0} folder", versionFileLocal), "Version information missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); 
            }*/
            if (Convert.ToInt32(shortVersionFromVrs) < Convert.ToInt32(shortVersionFromFile)) 
            { 
                DialogResult result = MessageBox.Show(String.Format("Un nuovo aggiornamento di track da versione {0} alla {1} è disponibile!!  Aggiornare ora?", longVersionFromVrs, longVersionFromFile), "Upgrade available", MessageBoxButtons.YesNo, MessageBoxIcon.Question); 
                switch (result) 
                { 
                    case DialogResult.Yes: { 
                        string pathToExecutable = Application.StartupPath.ToString(); 
                        //string pathToExecutable="C:\\Documents and Settings\\admin\\Documenti\\Visual Studio 2008\\Projects\\CP-100\\UpgradeCP100\\bin\\release";
                        string upgradeExecutable = "UpgradeCP100.exe"; 
                        string fullUpgradeExecutable = String.Format("{0}\\{1}", pathToExecutable, upgradeExecutable); 
                        if (File.Exists(fullUpgradeExecutable)) 
                        { 
                            ProcessStartInfo upgradeProcess = new ProcessStartInfo(fullUpgradeExecutable); 
                            upgradeProcess.WorkingDirectory = pathToExecutable; 
                            Process.Start(upgradeProcess); 
                            Environment.Exit(0); 
                        } 
                        else 
                        { 
                            DialogResult result2 = MessageBox.Show(String.Format("{0} not found in {1}.\nPlease check that this file exists then try the upgrade process again.", upgradeExecutable, pathToExecutable), "Upgrade Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                        } 
                        break; 
                    } 
                    default: 
                        { 
                            break; 
                        } 
                } 
            } 
            else 
            { 
                DialogResult result = MessageBox.Show(String.Format("There are no upgrades currently available.  You have the latest version of Countdown."), "No upgrade available", MessageBoxButtons.OK, MessageBoxIcon.Information); 
            } 
        }

        public bool checkSiteTest()
        {
            string RemoteDomain = "http://server-hp";
            try
            {
                IPHostEntry inetServer = Dns.GetHostEntry(RemoteDomain.Replace("http://", String.Empty));
            }
            catch
            {
             //   DialogResult result = MessageBox.Show(String.Format("Impossibile connettersi a {0}.\nPrego controllare la connessione e riprovare.", RemoteDomain), "Errore nel controllo aggiornamento", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                string versionFileUrl = "http://server-hp/test.txt";
                System.Net.WebClient client = new System.Net.WebClient();
                string yy = String.Format("{0}\\{1}", System.Environment.ExpandEnvironmentVariables("%TEMP%"), "test.txt");
                client.DownloadFile(versionFileUrl, yy);
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;
              //  MessageBoxIcon iconsToShow = MessageBoxIcon.Information;
                if (ex.Message.Contains("could not be resolved"))
                {
                    errorDetails = String.Format("Errore nel connettersi a {0}.\nPrego controllare la connessione e riprovare.", RemoteDomain);
                    //iconsToShow = MessageBoxIcon.Error;
                }
                else
                    if (ex.Message.Contains("404"))
                    {
                        errorDetails = "Il server per l'aggiornamento non è al momento disponibile.\nPlease try again later.";
                        //iconsToShow = MessageBoxIcon.Information;
                    }
                //DialogResult result = MessageBox.Show(String.Format("{0}", errorDetails), "Error downloading file", MessageBoxButtons.OK, iconsToShow); 
                return false;
            }

            string versionFileLocal = "";
            string shortVersionFromFile = "";
            string longVersionFromFile = "";
            string longVersionFromVrs = "";
            string shortVersionFromVrs = "";

            // if (File.Exists(config.VersionFileLocal)) 
            // {   
            versionFileLocal = String.Format("{0}\\{1}", System.Environment.ExpandEnvironmentVariables("%TEMP%"), "test.txt");
            TextReader tr = new StreamReader(versionFileLocal); string tempStr = tr.ReadLine();
            tr.Close();
            File.Delete(versionFileLocal);
            longVersionFromFile = tempStr;
            shortVersionFromFile = tempStr.Replace(".", String.Empty);
            Version vrs = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            longVersionFromVrs = String.Format("{0}.{1}.{2}.{3}", vrs.Major, vrs.Minor, vrs.MajorRevision, vrs.MinorRevision);
            shortVersionFromVrs = String.Format("{0}{1}{2}{3}", vrs.Major, vrs.Minor, vrs.MajorRevision, vrs.MinorRevision);
            /*    } 
                else
                {   
                    DialogResult result = MessageBox.Show(String.Format("Version file not found or not accessible.\nPlease check that you have permission to read the {0} folder", versionFileLocal), "Version information missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); 
                }*/
            if (Convert.ToInt32(shortVersionFromVrs) < Convert.ToInt32(shortVersionFromFile))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Management.Smo.Mail;



namespace SMS
{
    class Test
    {
        private string logFilePath = "C:\\SetupLog.txt";
        private string GetSql(string Name)
        {
            try
            {

                // Gets the current assembly.
                Assembly Asm = Assembly.GetExecutingAssembly();

                // Resources are named using a fully qualified name.
                Stream strm = Asm.GetManifestResourceStream(Asm.GetName().Name + "." + Name);

                // Reads the contents of the embedded file.
                StreamReader reader = new StreamReader(strm);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw ex;
            }
        }

        private void ExecuteSql(string serverName, string dbName, string Sql, string user, string password)
        {
            string connStr = "Server=" + serverName + ";uid=" + user + ";pwd=" + password + ";database=" + dbName + ";";
            SqlConnection myConnection = new SqlConnection(connStr);
            Sql = Sql.Replace("GO", "~");
            string[] q = Sql.Split('~');

            try
            {
                for (int i = 0; i < q.Length; i++)
                {

                    myConnection.Open();
                    SqlCommand cmd = new SqlCommand(q[i], myConnection);
                    cmd.ExecuteNonQuery();
                    myConnection.Close();
                }
            }
            catch (Exception ex)
            {
                   Log(ex.ToString());
            }
        }
        protected void AddDBTable(string serverName, string user, string password)
        {
            try
            {
                // Creates the database and installs the tables.
                string strScript = GetSql("sql.txt");
                ExecuteSql(serverName, "master", strScript, user, password);
            }
            catch (Exception ex)
            {
                //Reports any errors and abort.
                Log(ex.ToString());
                throw ex;
            }
        }

        public void Install()
        {
            //base.Install(stateSaver);
            Log("Setup started");
            AddDBTable("server", "sa", "esasoftware");

       //     RestoreDatabase("Track","","server","sa","esasoftware","","c:\\log.txt");

        }

      /*  public void RestoreDatabase(String databaseName, String filePath, String serverName, String userName, String password, String dataFilePath, String logFilePath)
        {
            Restore sqlRestore = new Restore();

            BackupDeviceItem deviceItem = new BackupDeviceItem(filePath, DeviceType.File);
            sqlRestore.Devices.Add(deviceItem);
            sqlRestore.Database = databaseName;

            ServerConnection connection = new ServerConnection(serverName, userName, password);
            Server sqlServer = new Server(connection);

            Database db = sqlServer.Databases[databaseName];
            sqlRestore.Action = RestoreActionType.Database;
            String dataFileLocation = dataFilePath + databaseName + ".mdf";
            String logFileLocation = logFilePath + databaseName + "_Log.ldf";
            db = sqlServer.Databases[databaseName];
            RelocateFile rf = new RelocateFile(databaseName, dataFileLocation);

            sqlRestore.RelocateFiles.Add(new RelocateFile(databaseName, dataFileLocation));
            sqlRestore.RelocateFiles.Add(new RelocateFile(databaseName + "_log", logFileLocation));
            sqlRestore.ReplaceDatabase = true;
            //sqlRestore.Complete += new ServerMessageEventHandler(sqlRestore_Complete);
            //sqlRestore.PercentCompleteNotification = 10;
            //sqlRestore.PercentComplete += new PercentCompleteEventHandler(sqlRestore_PercentComplete);

            sqlRestore.SqlRestore(sqlServer);

            db = sqlServer.Databases[databaseName];

            db.SetOnline();

            sqlServer.Refresh();
        }*/

        public void Log(string str)
        {
            StreamWriter Tex;
            try
            {
                Tex = File.AppendText(this.logFilePath);
                Tex.WriteLine(DateTime.Now.ToString() + " " + str);
                Tex.Close();
            }
            catch
            { }
        }
    }
}

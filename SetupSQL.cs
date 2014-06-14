using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.IO;
using System.Reflection;
using System.Data.SqlClient;



namespace SMS
{
    [RunInstaller(true)]
    public partial class SetupSQL : Installer
    {
        private string logFilePath = "C:\\SetupLog.txt";
        public SetupSQL()
        {
            InitializeComponent();
        }

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

        private void ExecuteSql(string serverName, string dbName, string Sql,string user,string password)
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
        protected void AddDBTable(string serverName,string user,string password)
        {
            try
            {
                // Creates the database and installs the tables.
                string strScript = GetSql("sql.txt");
                ExecuteSql(serverName, "master", strScript,user,password);
            }
            catch (Exception ex)
            {
                //Reports any errors and abort.
                Log(ex.ToString());
                throw ex;
            }
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
            Log("Setup started");
            AddDBTable(this.Context.Parameters["servername"], this.Context.Parameters["user"], this.Context.Parameters["password"]);

        }
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

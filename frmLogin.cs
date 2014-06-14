using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace SMS
{
    public partial class frmLogin : Form
    {
        public Form1 mainFrm;
       // bool start = false;
        public string CURRENT_LEVEL;
        public frmLogin()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            frmSQLServer frm = new frmSQLServer();
            frm.mainFrm = mainFrm;
            frm.ShowDialog();
            frmLogin_Load(null, null);
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            label6.Text = "Versione:" + Application.ProductVersion;

            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(path + "server.txt"))
            {
                string dd;
                using (StreamReader rd = new StreamReader(path + "server.txt"))
                {
                    dd = rd.ReadLine();
                }
                string[] CS = dd.Split(';');
                string[] pp = CS[0].Split('=');
                string server = pp[1];
                label5.Text = "Server selezionato: " + server;
            }
            else
            {
                label5.Text = "Server selezionato: Nessuno";
                //start = false;
            }

            /*UpdateComponent upd = new UpdateComponent();
            if (upd.checkSite())
                label7.Visible = true;
            else
                label7.Visible = false;*/
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (effettuaLogin(textBox1.Text, textBox2.Text) > 0)
            {
                button1.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                button1.DialogResult = DialogResult.None;
                MessageBox.Show(this, "Nome utente o password non valida. Impossibile effettuare l'accesso.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private int effettuaLogin(string user, string password)
        {
            string USER = Encryption.Encrypt(user, "2858security");
            string PASS = Encryption.Encrypt(password, "2858security");

            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
            string SQL = @"SELECT * FROM Utenti WHERE UserID='"+USER+"' AND Password='"+PASS+"'";

            SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
            System.Data.DataSet ds = new System.Data.DataSet();
            try
            {
                da.Fill(ds, "usr");

                if (ds.Tables["usr"].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables["usr"].Rows.Count; i++)
                    {
                        CURRENT_LEVEL = Encryption.Decrypt(ds.Tables["usr"].Rows[i]["Livello"].ToString(), "2858security");
                    }
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                MessageBox.Show("Errore nell'accesso al server. Il server non è valido. Provare selezionando un'altra base di dati dalle opzioni.");
                return 0;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                button1_Click(null, null);
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                button1_Click(null, null);
        }

        private void label7_Click(object sender, EventArgs e)
        {
           UpdateComponent upd = new UpdateComponent();
           upd.checkSite();
        }
    }
}

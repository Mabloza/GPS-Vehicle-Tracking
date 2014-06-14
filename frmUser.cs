using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace SMS
{
    public partial class frmUser : Form
    {
        public Form1 mainFrm;
        public frmUser()
        {
            InitializeComponent();
        }

        private void frmUser_Load(object sender, EventArgs e)
        {
            getLogin();
        }

        private void getLogin()
        {
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
            string SQL = @"SELECT * FROM Utenti";

            SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
            System.Data.DataSet ds = new System.Data.DataSet();
            da.Fill(ds, "usr");

            listBox1.Items.Clear();
            if (ds.Tables["usr"].Rows.Count > 0)
            {
                for (int i = 0; i < ds.Tables["usr"].Rows.Count; i++)
                {
                    string USER = Encryption.Decrypt(ds.Tables["usr"].Rows[i]["UserID"].ToString(), "2858security");
                    /*string PASS = Encryption.Encrypt(ds.Tables["usr"].Rows[i]["Password"].ToString(), "2858security");
                    string Livello = ds.Tables["usr"].Rows[i]["Livello"].ToString();*/

                    listBox1.Items.Add(USER);
                }
            }
        }

        private int getUser(string UID,bool onlyCheck)
        {
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
            string USER = Encryption.Encrypt(UID, "2858security");

            string SQL = @"SELECT * FROM Utenti WHERE UserID='"+USER+"'";

            SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
            System.Data.DataSet ds = new System.Data.DataSet();
            da.Fill(ds, "usr");

            if (ds.Tables["usr"].Rows.Count > 0)
            {
                if (!onlyCheck)
                {
                    for (int i = 0; i < ds.Tables["usr"].Rows.Count; i++)
                    {
                        string USR = Encryption.Decrypt(ds.Tables["usr"].Rows[i]["UserID"].ToString(), "2858security");
                        string PASS = Encryption.Decrypt(ds.Tables["usr"].Rows[i]["Password"].ToString(), "2858security");
                        string Livello = Encryption.Decrypt(ds.Tables["usr"].Rows[i]["Livello"].ToString(), "2858security");

                        textBox1.Text = USR;
                        textBox2.Text = PASS;
                        setLevel(Livello);
                    }
                    return 1;
                }
                else
                    return 1;
            }
            else
                return 0;
        }


        private void saveUser(string UID, string PASS)
        {
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);

            string USER = Encryption.Encrypt(UID, "2858security");
            string PASSW = Encryption.Encrypt(PASS, "2858security");

            string LEVEL = Encryption.Encrypt(getLevel(), "2858security");

            string SQL = @"INSERT INTO Utenti(UserID,Password,Livello) VALUES('"+USER+"','"+PASSW+"','"+LEVEL+"');";

            myConnection.Open();
            SqlCommand cmd = new SqlCommand(SQL, myConnection);
            if (cmd.ExecuteNonQuery()>0)
            {
                MessageBox.Show("Utente salvato correttamente.");
            }
            else
            {
                MessageBox.Show("Errore nel salvare l'utente.");
            }
        }

        private void updateUser(string UID, string PASS)
        {
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);

            string USER = Encryption.Encrypt(UID, "2858security");
            string PASSW = Encryption.Encrypt(PASS, "2858security");

            string LEVEL = Encryption.Encrypt(getLevel(), "2858security");

            string SQL = @"UPDATE Utenti SET Password ='"+PASSW+"' ,Livello ='"+LEVEL+"' WHERE UserID='"+USER+"'";

            myConnection.Open();
            SqlCommand cmd = new SqlCommand(SQL, myConnection);
            if (cmd.ExecuteNonQuery() > 0)
            {
                MessageBox.Show("Utente modificato correttamente.");
            }
            else
            {
                MessageBox.Show("Errore nel salvare l'utente.");
            }
        }

        private string getLevel()
        {
            string base_lvl = "0000";
           /*                  ^^^^
                               ||||--->Gestione mezzi
                               |||---->Report
                               ||----->Programmazioni
                               |------>Amministratore*/

            if (checkBox1.Checked) base_lvl = "1"; else base_lvl = "0";
            if (checkBox2.Checked) base_lvl += "1"; else base_lvl += "0";
            if (checkBox3.Checked) base_lvl += "1"; else base_lvl += "0";
            if (checkBox4.Checked) base_lvl += "1"; else base_lvl += "0";
            return base_lvl;
        }

        private void setLevel(string LVL)
        {
            if (LVL.Substring(0, 1) == "1") checkBox1.Checked = true; else checkBox1.Checked = false;
            if (LVL.Substring(1, 1) == "1") checkBox2.Checked = true; else checkBox2.Checked = false;
            if (LVL.Substring(2, 1) == "1") checkBox3.Checked = true; else checkBox3.Checked = false;
            if (LVL.Substring(3, 1) == "1") checkBox4.Checked = true; else checkBox4.Checked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox2.Checked = true;
                checkBox3.Checked = true;
                checkBox4.Checked = true;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex>-1)
                getUser(listBox1.SelectedItem.ToString(),false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (getUser(textBox1.Text, true)>0)
            {
                DialogResult res = MessageBox.Show(this,"Utente già presente nel database. Salvcare i cambiamenti?","Salvare", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes) updateUser(textBox1.Text, textBox2.Text);
            }
            else
            {
                saveUser(textBox1.Text, textBox2.Text);
                getLogin();
            }
        }
    }
}

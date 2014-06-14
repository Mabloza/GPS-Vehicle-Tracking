using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Moletrator.SQLDocumentor;

namespace SMS
{
    public partial class frmSQLServer : Form
    {
        public Form1 mainFrm;

        public frmSQLServer()
        {
            InitializeComponent();
        }

        private void buttonSQLServerEnumerator_Click(object sender, EventArgs e)
        {
            GetSQLDetails(this.listboxSQLServerInstances);
        }
        private bool SQLServerSelected()
        {
            if (listboxSQLServerInstances.SelectedIndex == -1)
            {
                return false;
            }
            else { return true; }
        }
        private bool UserDetailsEntered()
        {
            if ((textboxUserName.Text != "") && (textboxPassword.Text != ""))
            { return true; }
            else { return false; }
        }

        private void GetSQLDetails(ListBox SQLListBox)
        {
            SQLInfoEnumerator sie = new SQLInfoEnumerator();
            try
            {
                if (SQLListBox.Name == "listboxSQLServerDatabaseInstances")
                {
                    SQLListBox.Items.Clear();
                    sie.SQLServer = listboxSQLServerInstances.SelectedItem.ToString();
                    sie.Username = textboxUserName.Text;
                    sie.Password = textboxPassword.Text;
                    SQLListBox.Items.AddRange(sie.EnumerateSQLServersDatabases());
                }
                else
                {
                    SQLListBox.Items.Clear();
                    SQLListBox.Items.AddRange(sie.EnumerateSQLServers());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void buttonSQLServerDatabasesEnumerator_Click(object sender, EventArgs e)
        {
            if (SQLServerSelected())
            {
                if (UserDetailsEntered())
                {
                    GetSQLDetails(this.listboxSQLServerDatabaseInstances);
                }
                else { MessageBox.Show("A Username/Password Must Be Entered To View Database Information"); }
            }
            else { MessageBox.Show("A SQL Server Instance Must Be Selected To View Database Information"); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frmDatabaseInsert frm = new frmDatabaseInsert();
            frm.ShowDialog();
            listboxSQLServerInstances.Items.Add(frm.txtDatabase.Text);
            listboxSQLServerInstances.SelectedIndex = listboxSQLServerInstances.Items.Count-1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Server=192.168.2.200;uid=sa;pwd=esasoftware;database=Track;

            if (listboxSQLServerDatabaseInstances.SelectedIndex > -1 && listboxSQLServerInstances.SelectedIndex > -1)
            {
                string builder = "Server=" + listboxSQLServerInstances.SelectedItem.ToString() + ";";
                builder += "uid=" + textboxUserName.Text + ";";
                builder += "pwd=" + textboxPassword.Text + ";";
                builder += "database=" + listboxSQLServerDatabaseInstances.SelectedItem.ToString() + ";";
                mainFrm.saveNewConnectionString(builder);
                this.Close();
            }
        }
    }
}

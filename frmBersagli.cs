using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;

namespace SMS
{
    public partial class frmBersagli : Form
    {
        public Form1 mainFrm;

        public frmBersagli()
        {
            InitializeComponent();
        }

        private void frmBersagli_Load(object sender, EventArgs e)
        {
            updateObiettivi();
        }

        public void updateObiettivi()
        {
            dataGridView1.Rows.Clear();
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
            string today = DateTime.Now.ToShortDateString();
            string SQL = @"SELECT * FROM Obiettivi";

            SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "poi");

            dataGridView1.Rows.Add(ds.Tables["poi"].Rows.Count);
            for (int i = 0; i < ds.Tables["poi"].Rows.Count; i++)
            {

                string d = ds.Tables["poi"].Rows[i]["Descrizione"].ToString();
                string lat = ds.Tables["poi"].Rows[i]["Lat"].ToString();
                string lon = ds.Tables["poi"].Rows[i]["Lon"].ToString();

                dataGridView1.Rows[i].Cells[0].Value = d;
                dataGridView1.Rows[i].Cells[1].Value = lat;
                dataGridView1.Rows[i].Cells[2].Value = lon;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mainFrm.frmMapPoint.GetLocation = true;
            mainFrm.frmMapPoint.BringToFront();
            while (mainFrm.frmMapPoint.GetLocation)
            {
                Thread.Sleep(10);
            }
            mainFrm.frmMapPoint.SendToBack();
            MessageBox.Show(mainFrm.frmMapPoint.TempLocation.Latitude + " " + mainFrm.frmMapPoint.TempLocation.Longitude);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Data.SqlClient;

namespace SMS
{
    public partial class frmKmAnno : Form
    {
        public Form1 mainFrm;
        double[] MesiKm = new double[13];

        public frmKmAnno()
        {
            InitializeComponent();
        }

        private void frmKmAnno_Load(object sender, EventArgs e)
        {
            ElaboraStorico();
            CreateGraph(zedGraphControl1);
        }

        private void CreateGraph(ZedGraphControl zgc)
        {
            string[] mesi = { "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno", "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre" };
            GraphPane myPane = zgc.GraphPane;

            // Set the titles and axis labels
            myPane.Title.Text = "Km percorsi per anno";
            myPane.XAxis.Title.Text = "Mese";
            myPane.XAxis.Type = AxisType.Text;
            myPane.XAxis.Scale.TextLabels = mesi;
            myPane.YAxis.Title.Text = "Km percorsi";

            PointPairList list = new PointPairList();
            for (double x = 1; x < 13; x++)
            {
                double y = MesiKm[int.Parse(x.ToString())];
                list.Add(x, y, mesi[int.Parse((x-1).ToString())].ToString() +" "+ textBox1.Text);
            }

            BarItem bar =  myPane.AddBar(textBox1.Text, list, Color.Blue);
     
            // Fill the axis background with a color gradient
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45F);

            // Fill the pane background with a color gradient
            myPane.Fill = new Fill(Color.White, Color.FromArgb(220, 220, 255), 45F);

            // Calculate the Axis Scale Ranges
            zgc.AxisChange();
        }

        /*
            SELECT     ID, Vehicle, Position, DateTime
            FROM         Position
            WHERE     (CONVERT(DATETIME, DateTime, 103) > '01/01/2010') AND (CONVERT(DATETIME, DateTime, 103) < '01/02/2010') AND Vehicle = " + num.device_number + "'";
         */


        private void ElaboraStorico()
        {
            if (mainFrm.listBox1.SelectedItems.Count > 0)
            {
                if (mainFrm.listBox1.SelectedItems.Count == 1)
                {
                    mainFrm.updateConnectionString();
                    SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
                    myConnection.Open();
                    string Upd = "UPDATE Position SET DateTime = REPLACE(DateTime,'.',':');";
                    SqlCommand cmd = new SqlCommand(Upd, myConnection);
                    cmd.ExecuteNonQuery();
                    myConnection.Close();

                    smartrack num = mainFrm.searchDevicebyDescription(mainFrm.listBox1.SelectedItems[0].ToString());

                    string base_sql = @"SELECT ID, Vehicle, Position, DateTime FROM Position ";

                    for (int mese = 1; mese < 13; mese++)
                    {
                        string data;
                        string data2;

                        if (mese.ToString().Length == 1) 
                            data = "0"+mese +"/01/" + textBox1.Text;
                        else
                            data =  mese + "/01/" + textBox1.Text;

                        if ((mese + 1) < 13)
                        {
                            if ((mese + 1).ToString().Length == 1)
                                data2 = "0" + (mese + 1) + "/01/" + textBox1.Text;
                            else
                                data2 = (mese + 1) + "/01/" + textBox1.Text;
                        }
                        else
                        {
                            data2 = "01/01/" + (int.Parse(textBox1.Text) + 1).ToString();
                        }

                        string SQL = "WHERE (CONVERT(DATETIME, DateTime, 103) > '"+data+"') AND (CONVERT(DATETIME, DateTime, 103) < '"+data2+"') AND Vehicle = '" + num.device_number + "'";

                        SqlDataAdapter da = new SqlDataAdapter(base_sql+SQL, myConnection);
                        System.Data.DataSet ds = new System.Data.DataSet();
                        da.Fill(ds, "pos");

                        double Km = 0;
                        if (ds.Tables["pos"].Rows.Count > 0)
                        {
                            for (int i = 0; i < ds.Tables["pos"].Rows.Count; i++)
                            {
                                string position = "";
                                string position2 = "";
                                try
                                {
                                    position = ds.Tables["pos"].Rows[i]["Position"].ToString();
                                    position2 = ds.Tables["pos"].Rows[i + 1]["Position"].ToString();
                                }
                                catch { break; }

                                Km += calcolaKm(position, position2);
                            }
                            MesiKm[mese] = Km;
                        }
                        else
                            MesiKm[mese] = 0;
                    }
                }
            }
        }

        private double calcolaKm(string km1, string km2)
        {
            double TotalKM = 0;
            string point1 = km1;
            string point2 = km2;

            if (point1.IndexOf("$GPRMC") > -1 && point2.IndexOf("$GPRMC") > -1)
            {
                string[] p1 = mainFrm.returnNMEACoord(point1).Split('§');
                string[] p2 = mainFrm.returnNMEACoord(point2).Split('§');

                TotalKM += mainFrm.distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
            }

            if (point1.IndexOf("KTA") > -1 && point2.IndexOf("KTA") > -1)
            {
                string[] p1 = mainFrm.returnKFTCoord(point1).Split('§');
                string[] p2 = mainFrm.returnKFTCoord(point2).Split('§');

                TotalKM += mainFrm.distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
            }

            if (point1.IndexOf("\nHP=") > -1 && point2.IndexOf("\nHP=") > -1)
            {
                string[] p1 = mainFrm.returnCP100Coord(point1).Split('§');
                string[] p2 = mainFrm.returnCP100Coord(point2).Split('§');

                if ((p1[0] != "") && (p2[0] != ""))
                    TotalKM += mainFrm.distance(Double.Parse(p1[0]), Double.Parse(p1[1]), Double.Parse(p2[0]), Double.Parse(p2[1]), 'K');
                else
                    TotalKM += 0;
            }

            TotalKM = Math.Round(TotalKM, 3);
            if (TotalKM == 0.039) TotalKM = 0.0;
            return TotalKM;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                ElaboraStorico();
                CreateGraph(zedGraphControl1);
                this.Refresh();
            }
        }
    }
}

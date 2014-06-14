using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.VisualBasic;

namespace SMS
{
    public partial class frmSoste : Form
    {
        public Form1 mainFrm;

        public frmSoste()
        {
            InitializeComponent();
        }

        private void frmSoste_Load(object sender, EventArgs e)
        {
        }

        private void ElaboraSoste()
        {
            double RADIUS = 0.1;

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

                    string data;
                    string data2;
                    data = dateDal.Value.Month + "/" + dateDal.Value.Day + "/" + dateDal.Value.Year;
                    data2 = dateAl.Value.Month + "/" + dateAl.Value.Day + "/" + dateAl.Value.Year;

                    string SQL = "WHERE (CONVERT(DATETIME, DateTime, 103) > '" + data + "') AND (CONVERT(DATETIME, DateTime, 103) < '" + data2 + "') AND Vehicle = '" + num.device_number + "'";

                    SqlDataAdapter da = new SqlDataAdapter(base_sql + SQL, myConnection);
                    System.Data.DataSet ds = new System.Data.DataSet();
                    da.Fill(ds, "pos");

                    string first_valid = "";
                    string last_valid = "";
                    double Km = 0;
                    if (ds.Tables["pos"].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables["pos"].Rows.Count; i++)
                        {
                            string position = "";
                            string position2 = "";

                            Km = 0;
                            while (Km <= RADIUS)
                            {
                                try
                                {
                                    position = ds.Tables["pos"].Rows[i]["Position"].ToString();
                                    position2 = ds.Tables["pos"].Rows[i + 1]["Position"].ToString();
                                }
                                catch { break; }

                                Km = calcolaKm(position, position2);
                                if (Km <= RADIUS)
                                {
                                    if (first_valid == "")
                                        first_valid = ds.Tables["pos"].Rows[i]["DateTime"].ToString();
                                }
                                else
                                {
                                    if(first_valid != "")
                                        last_valid = ds.Tables["pos"].Rows[i]["DateTime"].ToString();
                                }
                                i++;
                            }

                            if(first_valid!="" && last_valid!="")
                            {
                                DateTime tempo1 = DateTime.Parse(first_valid);
                                DateTime tempo2 = DateTime.Parse(last_valid);
                                System.TimeSpan diffResult = tempo2.Subtract(tempo1);
                                if (diffResult.Minutes >= int.Parse(mainFrm.TEMPO_SOSTA))
                                {
                                    listBox1.Items.Add("Sosta dal: " + first_valid + " al " + last_valid+". Tempo:"+diffResult.Minutes+" min.");
                                }
                                first_valid = last_valid = "";
                            }
                        }
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

        private void button1_Click(object sender, EventArgs e)
        {
            ElaboraSoste();
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace SMS
{
    public partial class frmObiettivo : Form
    {
        public Form1 mainFrm;
        public bool olnyAdd;
        public MapPoint.Location currentLocation;

        public frmObiettivo()
        {
            InitializeComponent();
        }

        private void frmObiettivo_Load(object sender, EventArgs e)
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

            if (ds.Tables["poi"].Rows.Count > 0)
            {
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
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>-1)
                findAddress(dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString() + "|" + dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString() + "|" + dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString());
        }


        public void findAddress(string Address)
        {
            string[] POS = Address.Split('|');

            MapPoint.Map map = mainFrm.frmMapPoint.Map.ActiveMap;

            object result = null;
            //if(POS[0]=="" || POS[1] == "")
            if (POS.Length==1)
                result = map.ShowFindDialog(POS[0], MapPoint.GeoFindState.geoFindAddress, (int)mainFrm.Handle, false);
            else if (POS.Length>1)
                result = map.ShowFindDialog(POS[1] + ";" + POS[2], MapPoint.GeoFindState.geoFindLatLong, (int)mainFrm.Handle, false);

            //See if the result is null
            if (result != null)
            {
                //If the result is a Location type get the
                //Location directly
                if (result is MapPoint.Location)
                {
                    currentLocation = result as MapPoint.Location;
                }
                else if (result is MapPoint.Pushpin)
                {
                    //If this is a Pushpin type, first get the Pushpin
                    MapPoint.Pushpin pushpin = result as MapPoint.Pushpin;
                    //Then get the location
                    currentLocation = pushpin.Location;
                }
                txtDestination.Text = currentLocation.Name;
                addNewPOI(currentLocation.Name, Math.Round(currentLocation.Latitude,6).ToString(), Math.Round(currentLocation.Longitude,6).ToString());
            }
            else
            {
                MessageBox.Show("No locations found. Please verify the input.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            findAddress(txtDestination.Text);
        }

        public void addNewPOI(string Poi,string Lat,string Lon)
        {
            bool find = false;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                string rd = dataGridView1.Rows[i].Cells[0].Value.ToString();
                if (rd == Poi)
                {
                    find = true;
                    break;
                }
            }

            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                string _lat = dataGridView1.Rows[i].Cells[1].Value.ToString();
                string _lon = dataGridView1.Rows[i].Cells[2].Value.ToString();
                if (_lat == Lat && _lon == Lon)
                {
                    find = true;
                    break;
                }
            }

            if (!find)
            {
                mainFrm.updateConnectionString();
                SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
                string today = DateTime.Now.ToShortDateString();
                string SQL = @"INSERT INTO Obiettivi(Descrizione,Lat,Lon) VALUES('" + Poi + "','"+Lat+"','"+Lon+"')";
                myConnection.Open();
                SqlCommand da = new SqlCommand(SQL, myConnection);
                da.ExecuteNonQuery();
                myConnection.Close();
                updateObiettivi();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (olnyAdd == false)
            {
                //***************** associo l'obiettivo al mezzo ******************
                if (mainFrm.listBox1.SelectedIndex == -1)
                {
                    MessageBox.Show("Per associare un bersaglio è necessario selezionare prima il mezzo.");
                    return;
                }
                smartrack sm = mainFrm.searchDevicebyDescription(mainFrm.listBox1.SelectedItem.ToString());
                //sm.Bersaglio = currentLocation;

                string bersaglio = currentLocation.Name + "|" + currentLocation.Latitude + "|" + currentLocation.Longitude;

                sm.SetBersaglio(bersaglio);
                mainFrm.deviceCtrl1.refreshStatus();

                if (MessageBox.Show(this, "Bersaglio associato. Visualizzo anche l'itinerario?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    MapPoint.Map map = mainFrm.frmMapPoint.Map.ActiveMap;
                    map.ActiveRoute.Clear();

                    frmDriver frm = new frmDriver();
                    frm.loadProfileAutomatic(mainFrm.CURRENT_DRIVER_PROFILE, map.ActiveRoute.DriverProfile);

                    MapPoint.Location start = null;

                    mainFrm.frmMapPoint.Map.Units = MapPoint.GeoUnits.geoKm;
                    if (mainFrm.listBox1.SelectedIndex > -1)
                    {
                        smartrack p = mainFrm.sm_array[mainFrm.listBox1.SelectedIndex];
                        start = map.GetLocation(p.Lat, p.Lon, map.Altitude);

                    }

                    map.ActiveRoute.Waypoints.Add(start, "Partenza");
                    map.ActiveRoute.Waypoints.Add(currentLocation, "Arrivo");
                    try
                    {
                        map.ActiveRoute.Calculate();
                        map.ActiveRoute.Application.ItineraryVisible = false;

                        string Reso = "Viaggio da: " + start.Name + " a " + currentLocation.Name + "\r\n";

                        Reso += "Totale Km: " + map.ActiveRoute.Distance + ". Durata prevista tragitto: " + Math.Ceiling((map.ActiveRoute.DrivingTime * 24 * 60)) + " minuti.\r\n";
                        Reso += @"Costo totale (stimato in base al profilo di guida): " + Math.Round(map.ActiveRoute.Cost, 2) + "€";
                        if (!String.IsNullOrEmpty(mainFrm.CURRENT_DRIVER_PROFILE))
                            Reso += "\r\nProfilo di guida selezionato: " + System.IO.Path.GetFileName(mainFrm.CURRENT_DRIVER_PROFILE);
                        else
                            Reso += "\r\nProfilo di guida selezionato: nessuno";

                        if (MessageBox.Show(this, Reso + "\nAssocio l'obiettivo e salvo l'informazione?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                            {
                                using (StreamWriter wr = new StreamWriter(saveFileDialog1.FileName, true))
                                {
                                    wr.WriteLine("**************************************");
                                    wr.WriteLine(Reso);
                                    wr.WriteLine("**************************************");
                                }
                            }

                           /* //***************** associo l'obiettivo al mezzo ******************
                            smartrack sm = mainFrm.searchDevicebyDescription(mainFrm.listBox1.SelectedItem.ToString());
                            sm.Bersaglio = currentLocation;
                            mainFrm.deviceCtrl1.refreshStatus();*/
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Impossibile associare il bersaglio. Possibile causa:" + ex.Message);
                    }
                }
                this.Close();
            }
            else
                this.Close();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                try
                {
                    string Curr = dataGridView1[0, e.RowIndex].Value.ToString();

                    if (MessageBox.Show(this, "Confermi l'eliminazione del record: " + Curr + "?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        mainFrm.updateConnectionString();
                        SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
                        string today = DateTime.Now.ToShortDateString();
                        string SQL = @"DELETE FROM Obiettivi WHERE Descrizione='" + Curr + "'";
                        myConnection.Open();
                        SqlCommand da = new SqlCommand(SQL, myConnection);
                        int indx = da.ExecuteNonQuery();
                        myConnection.Close();

                        if (indx > 0) updateObiettivi();
                    }
                }
                catch { }
            }
        }

        public void setNewLocation(MapPoint.Location loc)
        {
            mainFrm.frmMapPoint.SendToBack();
            if(txtDestination.Text!="")
                addNewPOI(txtDestination.Text, Math.Round(loc.Latitude, 6).ToString(), Math.Round(loc.Longitude, 6).ToString());
            else
                addNewPOI(loc.Location.Name, Math.Round(loc.Latitude, 6).ToString(), Math.Round(loc.Longitude, 6).ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(txtDestination.Text=="")
                MessageBox.Show("Prima di selezionare il punto inserire la descrizione nella casella di testo in alto.");
            MessageBox.Show("Selezionare il punto con un doppio click sulla mappa.");
            mainFrm.frmMapPoint.GetLocation = true;
            mainFrm.frmMapPoint.BringToFront();
        }
    }
}

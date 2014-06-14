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
    public partial class frmDestination : Form
    {
        public Form1 mainFrm;
        public MapPoint.Location currentLocation;

        public frmDestination()
        {
            InitializeComponent();
        }

        public void updatePOI()
        {
            mainFrm.updateConnectionString();
            SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
            string today = DateTime.Now.ToShortDateString();
            string SQL = @"SELECT * FROM POI";

            SqlDataAdapter da = new SqlDataAdapter(SQL, myConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "poi");

            dataGridView1.Rows.Clear();

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

        private void frmDestination_Load(object sender, EventArgs e)
        {
            updatePOI();
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Rows[e.RowIndex].Cells[1].Value != null)
                findAddress(dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString());
            else
                findAddress(txtDestination.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString() + "-" + dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MapPoint.Map map = mainFrm.frmMapPoint.Map.ActiveMap;

            map.ActiveRoute.Clear();

            frmDriver frm = new frmDriver();
            frm.loadProfileAutomatic(mainFrm.CURRENT_DRIVER_PROFILE,map.ActiveRoute.DriverProfile);

            MapPoint.Location start = null;


            if (mainFrm.listBox1.SelectedIndex > -1 )
            {
                    smartrack p = mainFrm.sm_array[mainFrm.listBox1.SelectedIndex];
                    start = map.GetLocation(p.Lat, p.Lon, map.Altitude);
            }

            map.ActiveRoute.Waypoints.Add(start, "Partenza");
            map.ActiveRoute.Waypoints.Add(currentLocation, "Arrivo");

            map.ActiveRoute.DriverProfile.IncludeRestStops = true;
            try
            {
                map.ActiveRoute.Calculate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (map.ActiveRoute.IsCalculated)
            {
                addNewPOI(txtDestination.Text);
            }
        }

        public void addNewPOI(string Poi)
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

            if (!find)
            {
                Poi = Poi.Replace("'", " ");
                mainFrm.updateConnectionString();
                SqlConnection myConnection = new SqlConnection(mainFrm.connectionString);
                string today = DateTime.Now.ToShortDateString();
                string SQL = @"INSERT INTO POI(Descrizione,Lat,Lon) VALUES('" + Poi + "','','')";
                myConnection.Open();
                SqlCommand da = new SqlCommand(SQL, myConnection);
                da.ExecuteNonQuery();
                myConnection.Close();
            }
        }

        public void findAddress(string address)
        {
            MapPoint.Map map = mainFrm.frmMapPoint.Map.ActiveMap;

            //Call the ShowFindDialog to show the find (modal) dialog
            object result = map.ShowFindDialog(address, MapPoint.GeoFindState.geoFindDefault, (int)mainFrm.Handle, false);

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
                        string SQL = @"DELETE FROM POI WHERE Descrizione='" + Curr + "'";
                        myConnection.Open();
                        SqlCommand da = new SqlCommand(SQL, myConnection);
                        int indx = da.ExecuteNonQuery();
                        myConnection.Close();

                        if (indx > 0) updatePOI();
                    }
                }
                catch { }
            }
        }
    }
}

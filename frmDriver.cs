using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MapPoint;

namespace SMS
{
    public partial class frmDriver : Form
    {
        public frmMapPoint device;
        public Form1 mainFrm;

        public frmDriver()
        {
            InitializeComponent();
        }

        private void frmDriver_Load(object sender, EventArgs e)
        {
            loadeDriverProfile();
        }

        public GeoVolumeUnits getFuelTankUnits()
        {
             switch (txtFuelConsumptionUnits.Text)
            {
                case "Litri": return GeoVolumeUnits.geoLiters;
                case "Galloni (U.S.)": return GeoVolumeUnits.geoUSGallons;
                case "Galloni (U.K.)": return GeoVolumeUnits.geoUKGallons;
                default: return GeoVolumeUnits.geoLiters;
            }
        }
        public string setFuelTankUnits(string Val)
        {
            string str = "";
            switch (Val)
            {
                case "geoLiters": str = "Litri"; break;
                case "geoUSGallons": str = "Galloni (U.S.)"; break;
                case "geoUKGallons": str = "Galloni (U.K.)"; break;
            }
            return str;
        }

        public GeoFuelConsumptionUnits getFuelConsumptionUnits()
        {
            switch (txtFuelConsumptionUnits.Text)
            {
                case "Litri per 100 Km": return GeoFuelConsumptionUnits.geoLitersPer100Kilometers;
                case "Litri per 10 Km": return GeoFuelConsumptionUnits.geoLitersPer10Kilometers;
                case "Miglia per galloni (U.S.)": return GeoFuelConsumptionUnits.geoMilesPerGallonUS;
                case "Miglia per galloni (U.K.)": return GeoFuelConsumptionUnits.geoMilesPerGallonUK;
                default: return GeoFuelConsumptionUnits.geoLitersPer100Kilometers;
            }
        }

        public string setFuelConsumptionUnits(string Val)
        {
            string str = "";
            switch (Val)
            {
                case "geoLitersPer10Kilometers": str = "Litri per 10 Km"; break;
                case "geoLitersPer100Kilometers": str = "Litri per 100 Km"; break;
                case "geoMilesPerGallonUS": str = "Miglia per galloni (U.S.)"; break;
                case "geoMilesPerGallonUK": str = "Miglia per galloni (U.K.)"; break;
                default: str = "Litri per 100 Km"; break;
            }
            return str;
        }

        public void loadeDriverProfile()
        {
            txtFuelConsumptionUnits.Text = setFuelConsumptionUnits(device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionUnits.ToString());
            txtFuelTankCapacityUnit.Text = setFuelTankUnits(device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankUnits.ToString());
            txtFuelConsumtionCity.Text = device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionCity.ToString();
            txtFuelConsumptionHighway.Text = device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionHighway.ToString();
            txtFuelTankCapacity.Text = device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankCapacity.ToString();
            ckIncludeFuelWarnings.Checked = device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRefuelWarnings;
            ckIncludeRestStop.Checked = device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRestStops;
            Double test = device.Map.ActiveMap.ActiveRoute.DriverProfile.TankStartLevel * 100;
            valTankStartLevel.Value = int.Parse(test.ToString());
            test = device.Map.ActiveMap.ActiveRoute.DriverProfile.TankWarnLevel * 100;
            valTankWarnLevel.Value = int.Parse(Math.Round(test, 0).ToString());

            Double s = device.Map.ActiveMap.ActiveRoute.DriverProfile.TimeBetweenRests / GeoTimeConstants.geoOneHour;

            s = Math.Ceiling(s);
            txtTimeBetweenRests.Text = s.ToString();

            s = device.Map.ActiveMap.ActiveRoute.DriverProfile.RestStopDuration / GeoTimeConstants.geoOneHour;
            s = Math.Ceiling(s);
            txtRestStopDuration.Text = s.ToString();

            txtStartTime.Text = device.Map.ActiveMap.ActiveRoute.DriverProfile.StartTime.ToLongTimeString();
            txtStopTime.Text = device.Map.ActiveMap.ActiveRoute.DriverProfile.EndTime.ToLongTimeString();
        }

        public void saveDriverProfile()
        {
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionUnits = getFuelConsumptionUnits();
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankUnits = getFuelTankUnits();
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionCity = double.Parse(txtFuelConsumtionCity.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionHighway = double.Parse(txtFuelConsumptionHighway.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankCapacity = float.Parse(txtFuelTankCapacity.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRefuelWarnings = ckIncludeFuelWarnings.Checked;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRestStops = ckIncludeRestStop.Checked;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TimeBetweenRests = double.Parse(txtTimeBetweenRests.Text) * GeoTimeConstants.geoOneHour;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.RestStopDuration = double.Parse(txtRestStopDuration.Text) * GeoTimeConstants.geoOneHour;

            Double test = valTankStartLevel.Value / 100;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TankStartLevel = test;
            test = valTankWarnLevel.Value/100;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TankWarnLevel = test;

            string myStartTime = txtStartTime.Text;
            DateTime dtStartTime = DateTime.Parse(myStartTime);

            //End Time
            string myEndTime = txtStopTime.Text;
            DateTime dtEndTime = DateTime.Parse(myEndTime);

            device.Map.ActiveMap.ActiveRoute.DriverProfile.StartTime = dtStartTime;
            //Set the EndTime
            device.Map.ActiveMap.ActiveRoute.DriverProfile.EndTime = dtEndTime;
        }


        public MapPoint.DriverProfile returnDriverProfile()
        {
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionUnits = getFuelConsumptionUnits();
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankUnits = getFuelTankUnits();
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionCity = double.Parse(txtFuelConsumtionCity.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelConsumptionHighway = double.Parse(txtFuelConsumptionHighway.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.FuelTankCapacity = float.Parse(txtFuelTankCapacity.Text);
            device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRefuelWarnings = ckIncludeFuelWarnings.Checked;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.IncludeRestStops = ckIncludeRestStop.Checked;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TimeBetweenRests = double.Parse(txtTimeBetweenRests.Text) * GeoTimeConstants.geoOneHour;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.RestStopDuration = double.Parse(txtRestStopDuration.Text) * GeoTimeConstants.geoOneHour;

            Double test = valTankStartLevel.Value / 100;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TankStartLevel = test;
            test = valTankWarnLevel.Value / 100;
            device.Map.ActiveMap.ActiveRoute.DriverProfile.TankWarnLevel = test;

            string myStartTime = txtStartTime.Text;
            DateTime dtStartTime = DateTime.Parse(myStartTime);

            //End Time
            string myEndTime = txtStopTime.Text;
            DateTime dtEndTime = DateTime.Parse(myEndTime);

            device.Map.ActiveMap.ActiveRoute.DriverProfile.StartTime = dtStartTime;
            //Set the EndTime
            device.Map.ActiveMap.ActiveRoute.DriverProfile.EndTime = dtEndTime;

            return device.Map.ActiveMap.ActiveRoute.DriverProfile;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveDriverProfile();
            this.Close();
        }

        private void valTankStartLevel_ValueChanged(object sender, EventArgs e)
        {
            label8.Text = "Cap. serbatoio iniziale [" + valTankStartLevel.Value + "%]";
        }

        private void valTankWarnLevel_ValueChanged(object sender, EventArgs e)
        {
            label9.Text = "Limite attenzione serbatoio [" + valTankWarnLevel.Value + "%]";
        }

        private void saveProfileToFile()
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fname = saveFileDialog1.FileName;

                using (StreamWriter wr = new StreamWriter(fname))
                {
                    wr.WriteLine(txtFuelConsumptionUnits.Text);
                    wr.WriteLine(txtFuelTankCapacityUnit.Text);
                    wr.WriteLine(txtFuelConsumtionCity.Text);
                    wr.WriteLine(txtFuelConsumptionHighway.Text);
                    wr.WriteLine(txtFuelTankCapacity.Text);
                    wr.WriteLine(ckIncludeFuelWarnings.Checked); 
                    wr.WriteLine(ckIncludeRestStop.Checked);
                    wr.WriteLine(valTankStartLevel.Value); 
                    wr.WriteLine(valTankWarnLevel.Value);
                    wr.WriteLine(txtTimeBetweenRests.Text);
                    wr.WriteLine(txtRestStopDuration.Text);
                    wr.WriteLine(txtStartTime.Text);
                    wr.WriteLine(txtStopTime.Text);
                }
            }
        }

        public void loadProfileAutomatic(string fname, MapPoint.DriverProfile device)
        {
            if (!string.IsNullOrEmpty(fname))
            {
                using (StreamReader rd = new StreamReader(fname))
                {
                    txtFuelConsumptionUnits.Text = rd.ReadLine();
                    txtFuelTankCapacityUnit.Text = rd.ReadLine();
                    txtFuelConsumtionCity.Text = rd.ReadLine();
                    txtFuelConsumptionHighway.Text = rd.ReadLine();
                    txtFuelTankCapacity.Text = rd.ReadLine();
                    ckIncludeFuelWarnings.Checked = Boolean.Parse(rd.ReadLine());
                    ckIncludeRestStop.Checked = Boolean.Parse(rd.ReadLine());
                    valTankStartLevel.Value = int.Parse(rd.ReadLine());
                    valTankWarnLevel.Value = int.Parse(rd.ReadLine());
                    txtTimeBetweenRests.Text = rd.ReadLine();
                    txtRestStopDuration.Text = rd.ReadLine();
                    txtStartTime.Text = rd.ReadLine();
                    txtStopTime.Text = rd.ReadLine();
                }

                device.FuelConsumptionUnits = getFuelConsumptionUnits();
                device.FuelTankUnits = getFuelTankUnits();
                device.FuelConsumptionCity = double.Parse(txtFuelConsumtionCity.Text);
                device.FuelConsumptionHighway = double.Parse(txtFuelConsumptionHighway.Text);
                device.FuelTankCapacity = float.Parse(txtFuelTankCapacity.Text);
                device.IncludeRefuelWarnings = ckIncludeFuelWarnings.Checked;
                device.IncludeRestStops = ckIncludeRestStop.Checked;
                device.TimeBetweenRests = double.Parse(txtTimeBetweenRests.Text) * GeoTimeConstants.geoOneHour;
                device.RestStopDuration = double.Parse(txtRestStopDuration.Text) * GeoTimeConstants.geoOneHour;

                Double test = valTankStartLevel.Value / 100;
                device.TankStartLevel = test;
                test = valTankWarnLevel.Value / 100;
                device.TankWarnLevel = test;

                //Start Time
                string myStartTime = txtStartTime.Text;
                DateTime dtStartTime = DateTime.Parse(myStartTime);

                //End Time
                string myEndTime = txtStopTime.Text;
                DateTime dtEndTime = DateTime.Parse(myEndTime);

                device.StartTime = dtStartTime;
                //Set the EndTime
                device.EndTime = dtEndTime;
            }
        }

        public void loadProfileFromFile()
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fname = openFileDialog1.FileName;

                using (StreamReader rd = new StreamReader(fname))
                {
                    txtFuelConsumptionUnits.Text = rd.ReadLine();
                    txtFuelTankCapacityUnit.Text = rd.ReadLine();
                    txtFuelConsumtionCity.Text = rd.ReadLine();
                    txtFuelConsumptionHighway.Text = rd.ReadLine();
                    txtFuelTankCapacity.Text = rd.ReadLine();
                    ckIncludeFuelWarnings.Checked = Boolean.Parse(rd.ReadLine());
                    ckIncludeRestStop.Checked = Boolean.Parse(rd.ReadLine());
                    valTankStartLevel.Value = int.Parse(rd.ReadLine());
                    valTankWarnLevel.Value = int.Parse(rd.ReadLine());
                    txtTimeBetweenRests.Text = rd.ReadLine();
                    txtRestStopDuration.Text = rd.ReadLine();
                    txtStartTime.Text = rd.ReadLine();
                    txtStopTime.Text = rd.ReadLine();
                }
                mainFrm.CURRENT_DRIVER_PROFILE = fname;
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            loadProfileFromFile();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveProfileToFile();
        }


    }
}

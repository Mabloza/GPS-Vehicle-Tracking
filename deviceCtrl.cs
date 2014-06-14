using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SMS
{
    public partial class deviceCtrl : UserControl
    {
        smartrack device;
        public int DeviceINDEX;
        public Form1 mainFrm;

        public deviceCtrl()
        {
            InitializeComponent();
        }

        public void setDevice(smartrack dev)
        {
            this.device = dev;
            refreshStatus();
        }

        public void refreshStatus()
        {
            txtDescription.Text = device.Description;
            txtTarga.Text = device.Targa;
           // GPRSMode.Checked = device.GPRSMode;
            //SMSMode.Checked = device.SMSMode;
            txtPassword.Text = device.devicePassword;

            txtDevNumber.Text = device.device_number;

            txtDeviceType.Text = device.DeviceType;
            txtPolling.Text = device.Intervallo;

            //txtAPN.Text = device.APN;
            //cbAllarmi.Checked = device.AlarmMode== "on";
            //cbMovimento.Checked = device.AllarmeMovimento;
            //txtRaggio.Text = device.Raggio;
            //cbVelocita.Checked = device.AllarmeVelocita;
            //txtVelocita.Text = device.Velocita;
           // cbBloccaCarburante.Checked = !device.Carburante;
            //cbBloccaEnergia.Checked = !device.Energia;
            //cbPolling.Checked = device.PollingMode;
            //txtIntervallo.Text = device.Intervallo;
            //txtVolte.Text = device.Volte;
            if (device.Bersaglio == null || string.IsNullOrEmpty(device.Bersaglio))
                txtBersaglio.Text = "nessuno";
            else
            {
                string[] sp = device.Bersaglio.Split('|');
                txtBersaglio.Text =sp[0] + "(" + sp[1] + " " + sp[2] + ")";
            }

            if (System.IO.File.Exists(device.image))
                pictureBox1.Image = Image.FromFile(device.image);
            else
                pictureBox1.Image = null;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            device.initDevice();
            mainFrm.setFullBar(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            device.Logout();
            mainFrm.setFullBar(true);
        }

        private void SMSMode_CheckedChanged(object sender, EventArgs e)
        {
          /*  if (SMSMode.Checked)
            {
                if(device!=null)
                    device.useSMS();
            }*/
        }

        private void GPRSMode_CheckedChanged(object sender, EventArgs e)
        {
           /* if (GPRSMode.Checked)
                device.useGPRS();*/
        }

        private void button1_Click(object sender, EventArgs e)
        {
            device.setNewPassword(txtPassword.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
           // device.setAPN(txtAPN.Text);
            device.Description = txtDescription.Text;
            mainFrm.saveDevice(device, DeviceINDEX);
        }

        private void cbAllarmi_CheckedChanged(object sender, EventArgs e)
        {
            /*if (cbAllarmi.Checked)
                device.setAlarm("on");
            else
                device.setAlarm("off");*/
        }

        private void cbMovimento_CheckedChanged(object sender, EventArgs e)
        {
          /*  if (cbMovimento.Checked)
            {
                if(txtRaggio.Text!="")
                    device.allarmeMovimento(txtRaggio.Text);
            }
            else
            {
                device.disabilitaAllarmeMovimento();
            }*/
        }

        private void cbVelocita_CheckedChanged(object sender, EventArgs e)
        {
          /*  if (cbVelocita.Checked)
            {
                if(txtVelocita.Text!="")
                    device.allarmeVelocita(txtVelocita.Text);
            }
            else
                device.disabilitaVelocita();*/
        }

        private void cbBloccaCarburante_CheckedChanged(object sender, EventArgs e)
        {
          /*  if (cbBloccaCarburante.Checked)
                device.bloccaCarburante();
            else
                device.attivaCarburante();*/
        }

        private void cbBloccaEnergia_CheckedChanged(object sender, EventArgs e)
        {
           /* if (cbBloccaEnergia.Checked)
                device.bloccaEnergia();
            else
                device.attivaEnergia();*/
        }

        private void cbPolling_CheckedChanged(object sender, EventArgs e)
        {
            /*if (cbPolling.Checked)
            {
                if (txtIntervallo.Text != "" && txtVolte.Text != "")
                    device.setPolling(txtIntervallo.Text, txtVolte.Text);
            }
            else
            {
                device.stopPolling();
            }*/
        }

        private void button5_Click(object sender, EventArgs e)
        {
            mainFrm.saveDevice(device, DeviceINDEX);
            mainFrm.loadDevices();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (device != null)
            {
                device.requestPosition();
                mainFrm.setFullBar(true);
            }
            else
            {
                MessageBox.Show("E' necessario selezionare prima il dispositivo.");
            }
        }

        private void txtRaggio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                cbMovimento_CheckedChanged(null, null);
            }
        }

        private void txtVelocita_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                cbVelocita_CheckedChanged(null, null);
        }

        private void txtIntervallo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                cbPolling_CheckedChanged(null, null);
        }

        private void txtVolte_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                cbPolling_CheckedChanged(null, null);
        }

        private void SMSMode_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                device.image = openFileDialog1.FileName;
                pictureBox1.Image = Image.FromFile(device.image);
            }
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            device.Description = txtDescription.Text;
        }

        private void cbAllarmi_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void cbBloccaCarburante_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            /*string[] uscita = comboBox1.Text.Split('-');
            device.settaUscitaON(uscita[0].Replace(" ",""));
            int i = comboBox1.SelectedIndex;
            comboBox1.Items.RemoveAt(i);
            comboBox1.Items.Insert(i, uscita[0] + "- Attivata");*/
        }

        private void button9_Click(object sender, EventArgs e)
        {
          /*  string[] uscita = comboBox1.Text.Split('-');
            device.settaUscitaOFF(uscita[0].Replace(" ",""));
            int i = comboBox1.SelectedIndex;
            comboBox1.Items.RemoveAt(i);
            comboBox1.Items.Insert(i, uscita[0] + "- Disattivata");*/
        }

        private void button10_Click(object sender, EventArgs e)
        {
            device.device_number = txtDevNumber.Text;
            
        }

        private void button11_Click(object sender, EventArgs e)
        {
            device.Targa = txtTarga.Text;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            device.setPolling(txtPolling.Text);
        }

        private void txtDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            device.DeviceType = txtDeviceType.Text;
            if (txtDeviceType.Text == "KFT") button3.Visible = true;
            else button3.Visible = false;

          /*  if (device.DeviceType == "CP-100")
                btnInit.Visible = true;
            else
                btnInit.Visible = false;*/
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button13_Click(object sender, EventArgs e)
        {
        }

        private void button13_Click_1(object sender, EventArgs e)
        {
            device.Bersaglio = null;
            refreshStatus();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Inizializzo il dispositivo?", "Avvertenza",MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                device.initDevice();
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            device.initDevice();
        }
    }
}

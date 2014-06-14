using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace SMS
{
    public partial class frmMessaggi : Form
    {
        public NetworkStream sock;

        public void init(NetworkStream s)
        {
            sock = s;
        }

        public frmMessaggi()
        {
            InitializeComponent();
        }

        private void sendMsg(string txt)
        {
            StreamWriter wr = new StreamWriter(sock);
            wr.Write(txt + "$");
            wr.Flush();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "EFFETTUARE BASSA";
            txtMessage.Focus();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "RECARSI PRESSO ";
            txtMessage.Focus();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "TUTTO REGOLARE";
            txtMessage.Focus();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "OK";
            txtMessage.Focus();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "ANNULLA OPERAZIONE";
            txtMessage.Focus();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            txtMessage.Text = "CONTATTARE ";
            txtMessage.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            sendMsg(txtMessage.Text);
            this.Close();
        }

        private void frmMessaggi_Load(object sender, EventArgs e)
        {

        }
    }
}

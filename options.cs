using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SMS
{
    public partial class options : Form
    {
        public Form1 mainFrm;

        public options()
        {
            InitializeComponent();
        }

        private void options_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            mainFrm.saveOptions(textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text,txtRadius.Text,txtSosta.Text);
            this.Close();
        }
    }
}

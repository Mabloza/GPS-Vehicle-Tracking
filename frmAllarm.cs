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
    public partial class frmAllarm : Form
    {
        public Form1 mainFrm;

        private delegate void SetNewAllarm(string DateTime, string Number, string Message);

        public frmAllarm()
        {
            InitializeComponent();
        }



        public void Allarm(string DateTime, string Number, string Message)
        {
            if (this.dataGridView1.InvokeRequired)
            {
                SetNewAllarm stc = new SetNewAllarm(Allarm);
                this.Invoke(stc, new object[] { DateTime, Number, Message });
            }
            else
            {
                smartrack pp = mainFrm.searchDevice(Number);

                dataGridView1.Rows.Add();
                if (pp != null)
                {
                    DataGridViewImageCell displayCell = null;
                    displayCell = (DataGridViewImageCell)dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0];
                    displayCell.Value = Image.FromFile(pp.image);

                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[1].Value = DateTime;
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[2].Value = pp.Description + " (" + Number + ")";
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[3].Value = Message;
                }
                else
                {
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[1].Value = DateTime;
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[2].Value = "(" + Number + ")";
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[3].Value = Message;
                }
            }
        }

        private void Allarm(string DateTime, string Number, string Message, params object[] args)
        {
            string DT = string.Format(DateTime, args);
            string Num = string.Format(Number, args);
            string Mess = string.Format(Message, args);
            Allarm(DT, Num, Mess);
        }


        public void addNewAllarm(string DateTime, string Number, string Message)
        {
            Allarm(DateTime, Number, Message);
        }

        private void frmAllarm_Load(object sender, EventArgs e)
        {
            
        }
 

        private void dataGridView1_KeyPress(object sender, KeyPressEventArgs e)
        {
            const int SPACE = 32;
            if (e.KeyChar == SPACE)
            {
                if(dataGridView1.CurrentRow!=null && dataGridView1.CurrentRow.Index != -1)
                    dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
            }
        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                string CurrString = dataGridView1[2, e.RowIndex].Value.ToString();
                string[] tmp = CurrString.Split('(');
                tmp = tmp[1].Split(')');
                string CurrNumber = tmp[0];
                mainFrm.showDevice(CurrNumber);
            }
        }
    }
}

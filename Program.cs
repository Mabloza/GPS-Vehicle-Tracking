using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SMS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


          //  Test f = new Test();
           // f.Install();

            frmLogin frmlogin = new frmLogin();
            frmlogin.mainFrm = new Form1("0000");
            DialogResult res = frmlogin.ShowDialog();
            if(res!= DialogResult.Cancel || !string.IsNullOrEmpty(frmlogin.CURRENT_LEVEL))
                Application.Run(new Form1(frmlogin.CURRENT_LEVEL));

         //   Application.Run(new Form1("1111"));
        }
    }
}

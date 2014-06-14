namespace SMS
{
    partial class frmSQLServer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSQLServer));
            this.buttonSQLServerDatabasesEnumerator = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.listboxSQLServerDatabaseInstances = new System.Windows.Forms.ListBox();
            this.textboxPassword = new System.Windows.Forms.TextBox();
            this.textboxUserName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonSQLServerEnumerator = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listboxSQLServerInstances = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonSQLServerDatabasesEnumerator
            // 
            this.buttonSQLServerDatabasesEnumerator.Location = new System.Drawing.Point(247, 163);
            this.buttonSQLServerDatabasesEnumerator.Name = "buttonSQLServerDatabasesEnumerator";
            this.buttonSQLServerDatabasesEnumerator.Size = new System.Drawing.Size(25, 20);
            this.buttonSQLServerDatabasesEnumerator.TabIndex = 19;
            this.buttonSQLServerDatabasesEnumerator.Text = "...";
            this.buttonSQLServerDatabasesEnumerator.Click += new System.EventHandler(this.buttonSQLServerDatabasesEnumerator_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(12, 166);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 15);
            this.label4.TabIndex = 18;
            this.label4.Text = "SQL Server";
            // 
            // listboxSQLServerDatabaseInstances
            // 
            this.listboxSQLServerDatabaseInstances.Location = new System.Drawing.Point(100, 158);
            this.listboxSQLServerDatabaseInstances.Name = "listboxSQLServerDatabaseInstances";
            this.listboxSQLServerDatabaseInstances.Size = new System.Drawing.Size(135, 95);
            this.listboxSQLServerDatabaseInstances.TabIndex = 17;
            // 
            // textboxPassword
            // 
            this.textboxPassword.Location = new System.Drawing.Point(100, 129);
            this.textboxPassword.Name = "textboxPassword";
            this.textboxPassword.PasswordChar = '*';
            this.textboxPassword.Size = new System.Drawing.Size(135, 20);
            this.textboxPassword.TabIndex = 16;
            // 
            // textboxUserName
            // 
            this.textboxUserName.Location = new System.Drawing.Point(100, 102);
            this.textboxUserName.Name = "textboxUserName";
            this.textboxUserName.Size = new System.Drawing.Size(135, 20);
            this.textboxUserName.TabIndex = 15;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(12, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Password";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 105);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "User Name";
            // 
            // buttonSQLServerEnumerator
            // 
            this.buttonSQLServerEnumerator.Location = new System.Drawing.Point(247, 13);
            this.buttonSQLServerEnumerator.Name = "buttonSQLServerEnumerator";
            this.buttonSQLServerEnumerator.Size = new System.Drawing.Size(68, 20);
            this.buttonSQLServerEnumerator.TabIndex = 12;
            this.buttonSQLServerEnumerator.Text = "Cerca";
            this.buttonSQLServerEnumerator.Click += new System.EventHandler(this.buttonSQLServerEnumerator_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 15);
            this.label1.TabIndex = 11;
            this.label1.Text = "SQL Server";
            // 
            // listboxSQLServerInstances
            // 
            this.listboxSQLServerInstances.Location = new System.Drawing.Point(100, 13);
            this.listboxSQLServerInstances.Name = "listboxSQLServerInstances";
            this.listboxSQLServerInstances.Size = new System.Drawing.Size(135, 82);
            this.listboxSQLServerInstances.TabIndex = 10;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(247, 263);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 20;
            this.button1.Text = "Salva";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(247, 39);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(68, 23);
            this.button2.TabIndex = 21;
            this.button2.Text = "Inserisci";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // frmSQLServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(327, 298);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonSQLServerDatabasesEnumerator);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.listboxSQLServerDatabaseInstances);
            this.Controls.Add(this.textboxPassword);
            this.Controls.Add(this.textboxUserName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonSQLServerEnumerator);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listboxSQLServerInstances);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmSQLServer";
            this.Text = "Seleziona basi di dati";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSQLServerDatabasesEnumerator;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listboxSQLServerDatabaseInstances;
        private System.Windows.Forms.TextBox textboxPassword;
        private System.Windows.Forms.TextBox textboxUserName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonSQLServerEnumerator;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listboxSQLServerInstances;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
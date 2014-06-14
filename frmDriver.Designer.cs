namespace SMS
{
    partial class frmDriver
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmDriver));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtFuelConsumptionUnits = new System.Windows.Forms.ComboBox();
            this.txtFuelConsumtionCity = new System.Windows.Forms.TextBox();
            this.txtFuelConsumptionHighway = new System.Windows.Forms.TextBox();
            this.txtFuelTankCapacity = new System.Windows.Forms.TextBox();
            this.txtFuelTankCapacityUnit = new System.Windows.Forms.ComboBox();
            this.ckIncludeFuelWarnings = new System.Windows.Forms.CheckBox();
            this.ckIncludeRestStop = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.valTankWarnLevel = new System.Windows.Forms.TrackBar();
            this.valTankStartLevel = new System.Windows.Forms.TrackBar();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtTimeBetweenRests = new System.Windows.Forms.TextBox();
            this.txtRestStopDuration = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.txtStartTime = new System.Windows.Forms.MaskedTextBox();
            this.txtStopTime = new System.Windows.Forms.MaskedTextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.valTankWarnLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valTankStartLevel)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Unit. Consumo Carburante";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Consumo strade cittadine";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(145, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Consumo strade extra-urbane";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 169);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Capacità serbatoio";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 131);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(120, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Unit. Capacità serbatoio";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(346, 128);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(109, 13);
            this.label8.TabIndex = 7;
            this.label8.Text = "Cap. serbatoio iniziale";
            // 
            // txtFuelConsumptionUnits
            // 
            this.txtFuelConsumptionUnits.FormattingEnabled = true;
            this.txtFuelConsumptionUnits.Items.AddRange(new object[] {
            "Litri per 100 Km",
            "Litri per 10 Km",
            "Miglia per galloni (U.S.) ",
            "Miglia per galloni (U.K.)"});
            this.txtFuelConsumptionUnits.Location = new System.Drawing.Point(163, 18);
            this.txtFuelConsumptionUnits.Name = "txtFuelConsumptionUnits";
            this.txtFuelConsumptionUnits.Size = new System.Drawing.Size(121, 21);
            this.txtFuelConsumptionUnits.TabIndex = 8;
            // 
            // txtFuelConsumtionCity
            // 
            this.txtFuelConsumtionCity.Location = new System.Drawing.Point(163, 55);
            this.txtFuelConsumtionCity.Name = "txtFuelConsumtionCity";
            this.txtFuelConsumtionCity.Size = new System.Drawing.Size(121, 20);
            this.txtFuelConsumtionCity.TabIndex = 9;
            // 
            // txtFuelConsumptionHighway
            // 
            this.txtFuelConsumptionHighway.Location = new System.Drawing.Point(163, 90);
            this.txtFuelConsumptionHighway.Name = "txtFuelConsumptionHighway";
            this.txtFuelConsumptionHighway.Size = new System.Drawing.Size(121, 20);
            this.txtFuelConsumptionHighway.TabIndex = 10;
            // 
            // txtFuelTankCapacity
            // 
            this.txtFuelTankCapacity.Location = new System.Drawing.Point(163, 166);
            this.txtFuelTankCapacity.Name = "txtFuelTankCapacity";
            this.txtFuelTankCapacity.Size = new System.Drawing.Size(121, 20);
            this.txtFuelTankCapacity.TabIndex = 11;
            // 
            // txtFuelTankCapacityUnit
            // 
            this.txtFuelTankCapacityUnit.FormattingEnabled = true;
            this.txtFuelTankCapacityUnit.Items.AddRange(new object[] {
            "Litri",
            "Galloni (U.S.)",
            "Galloni (U.K.)"});
            this.txtFuelTankCapacityUnit.Location = new System.Drawing.Point(163, 128);
            this.txtFuelTankCapacityUnit.Name = "txtFuelTankCapacityUnit";
            this.txtFuelTankCapacityUnit.Size = new System.Drawing.Size(121, 21);
            this.txtFuelTankCapacityUnit.TabIndex = 12;
            // 
            // ckIncludeFuelWarnings
            // 
            this.ckIncludeFuelWarnings.AutoSize = true;
            this.ckIncludeFuelWarnings.Location = new System.Drawing.Point(349, 17);
            this.ckIncludeFuelWarnings.Name = "ckIncludeFuelWarnings";
            this.ckIncludeFuelWarnings.Size = new System.Drawing.Size(156, 17);
            this.ckIncludeFuelWarnings.TabIndex = 13;
            this.ckIncludeFuelWarnings.Text = "Visualizza avvisi carburante";
            this.ckIncludeFuelWarnings.UseVisualStyleBackColor = true;
            // 
            // ckIncludeRestStop
            // 
            this.ckIncludeRestStop.AutoSize = true;
            this.ckIncludeRestStop.Location = new System.Drawing.Point(349, 40);
            this.ckIncludeRestStop.Name = "ckIncludeRestStop";
            this.ckIncludeRestStop.Size = new System.Drawing.Size(135, 17);
            this.ckIncludeRestStop.TabIndex = 14;
            this.ckIncludeRestStop.Text = "Visualizza pause riposo";
            this.ckIncludeRestStop.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(346, 173);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(132, 13);
            this.label9.TabIndex = 17;
            this.label9.Text = "Limite attenzione serbatoio";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(573, 311);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(92, 23);
            this.button1.TabIndex = 18;
            this.button1.Text = "Salva e Chiudi";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // valTankWarnLevel
            // 
            this.valTankWarnLevel.Location = new System.Drawing.Point(501, 166);
            this.valTankWarnLevel.Maximum = 100;
            this.valTankWarnLevel.Name = "valTankWarnLevel";
            this.valTankWarnLevel.Size = new System.Drawing.Size(157, 45);
            this.valTankWarnLevel.TabIndex = 16;
            this.valTankWarnLevel.ValueChanged += new System.EventHandler(this.valTankWarnLevel_ValueChanged);
            // 
            // valTankStartLevel
            // 
            this.valTankStartLevel.Location = new System.Drawing.Point(501, 118);
            this.valTankStartLevel.Maximum = 100;
            this.valTankStartLevel.Name = "valTankStartLevel";
            this.valTankStartLevel.Size = new System.Drawing.Size(157, 45);
            this.valTankStartLevel.TabIndex = 15;
            this.valTankStartLevel.ValueChanged += new System.EventHandler(this.valTankStartLevel_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(346, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(128, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Tempo max di guida (Ore)";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(346, 96);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(130, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "Tempo max di riposo (Min)";
            // 
            // txtTimeBetweenRests
            // 
            this.txtTimeBetweenRests.Location = new System.Drawing.Point(510, 65);
            this.txtTimeBetweenRests.Name = "txtTimeBetweenRests";
            this.txtTimeBetweenRests.Size = new System.Drawing.Size(136, 20);
            this.txtTimeBetweenRests.TabIndex = 21;
            // 
            // txtRestStopDuration
            // 
            this.txtRestStopDuration.Location = new System.Drawing.Point(510, 93);
            this.txtRestStopDuration.Name = "txtRestStopDuration";
            this.txtRestStopDuration.Size = new System.Drawing.Size(136, 20);
            this.txtRestStopDuration.TabIndex = 22;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(15, 212);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(85, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "Ora inizio tragitto";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(15, 245);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(79, 13);
            this.label11.TabIndex = 24;
            this.label11.Text = "Ora fine tragitto";
            // 
            // txtStartTime
            // 
            this.txtStartTime.Location = new System.Drawing.Point(163, 209);
            this.txtStartTime.Mask = "00:00:00";
            this.txtStartTime.Name = "txtStartTime";
            this.txtStartTime.Size = new System.Drawing.Size(57, 20);
            this.txtStartTime.TabIndex = 25;
            // 
            // txtStopTime
            // 
            this.txtStopTime.Location = new System.Drawing.Point(163, 242);
            this.txtStopTime.Mask = "00:00:00";
            this.txtStopTime.Name = "txtStopTime";
            this.txtStopTime.Size = new System.Drawing.Size(57, 20);
            this.txtStopTime.TabIndex = 26;
            this.txtStopTime.ValidatingType = typeof(System.DateTime);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(12, 311);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(88, 23);
            this.button2.TabIndex = 27;
            this.button2.Text = "Carica profilo";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(106, 311);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(88, 23);
            this.button3.TabIndex = 28;
            this.button3.Text = "Salva profilo";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "*.dps";
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Driver Profile|*.dps|Tutti i file|*.*";
            this.openFileDialog1.Title = "Seleziona il file da aprire";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "*.dps";
            this.saveFileDialog1.Filter = "Driver Profile|*.dps|Tutti i file|*.*";
            this.saveFileDialog1.Title = "Salva il profilo di guida";
            // 
            // frmDriver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(677, 346);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtStopTime);
            this.Controls.Add(this.txtStartTime);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtRestStopDuration);
            this.Controls.Add(this.txtTimeBetweenRests);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.valTankWarnLevel);
            this.Controls.Add(this.valTankStartLevel);
            this.Controls.Add(this.ckIncludeRestStop);
            this.Controls.Add(this.ckIncludeFuelWarnings);
            this.Controls.Add(this.txtFuelTankCapacityUnit);
            this.Controls.Add(this.txtFuelTankCapacity);
            this.Controls.Add(this.txtFuelConsumptionHighway);
            this.Controls.Add(this.txtFuelConsumtionCity);
            this.Controls.Add(this.txtFuelConsumptionUnits);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmDriver";
            this.Text = "Profilo di guida";
            this.Load += new System.EventHandler(this.frmDriver_Load);
            ((System.ComponentModel.ISupportInitialize)(this.valTankWarnLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valTankStartLevel)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox txtFuelConsumptionUnits;
        private System.Windows.Forms.TextBox txtFuelConsumtionCity;
        private System.Windows.Forms.TextBox txtFuelConsumptionHighway;
        private System.Windows.Forms.TextBox txtFuelTankCapacity;
        private System.Windows.Forms.ComboBox txtFuelTankCapacityUnit;
        private System.Windows.Forms.CheckBox ckIncludeFuelWarnings;
        private System.Windows.Forms.CheckBox ckIncludeRestStop;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TrackBar valTankWarnLevel;
        private System.Windows.Forms.TrackBar valTankStartLevel;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtTimeBetweenRests;
        private System.Windows.Forms.TextBox txtRestStopDuration;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.MaskedTextBox txtStartTime;
        private System.Windows.Forms.MaskedTextBox txtStopTime;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}
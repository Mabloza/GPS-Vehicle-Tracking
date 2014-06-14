namespace SMS
{
    partial class frmMapPoint
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMapPoint));
            this.Map = new AxMapPoint.AxMappointControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mappaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.datiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.politicaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.syradeEDatiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.terrenoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viasualizzaOnlineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.misuraDistanzaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.salvaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.salvaComeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.direzioneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mappaToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.mappaEDirezioneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.Map)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Map
            // 
            this.Map.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.Map.Enabled = true;
            this.Map.Location = new System.Drawing.Point(1, 28);
            this.Map.Name = "Map";
            this.Map.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("Map.OcxState")));
            this.Map.Size = new System.Drawing.Size(865, 443);
            this.Map.TabIndex = 0;
            this.Map.BeforeDblClick += new AxMapPoint._IMappointCtrlEvents_BeforeDblClickEventHandler(this.Map_BeforeDblClick);
            this.Map.MouseMoveEvent += new AxMapPoint._IMappointCtrlEvents_MouseMoveEventHandler(this.Map_MouseMoveEvent);
            this.Map.LocationChanged += new System.EventHandler(this.Map_LocationChanged);
            this.Map.BeforeClick += new AxMapPoint._IMappointCtrlEvents_BeforeClickEventHandler(this.Map_BeforeClick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mappaToolStripMenuItem,
            this.salvaToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(866, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mappaToolStripMenuItem
            // 
            this.mappaToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stileToolStripMenuItem,
            this.viasualizzaOnlineToolStripMenuItem,
            this.toolStripMenuItem1,
            this.misuraDistanzaToolStripMenuItem});
            this.mappaToolStripMenuItem.Name = "mappaToolStripMenuItem";
            this.mappaToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.mappaToolStripMenuItem.Text = "Mappa";
            // 
            // stileToolStripMenuItem
            // 
            this.stileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.datiToolStripMenuItem,
            this.notteToolStripMenuItem,
            this.politicaToolStripMenuItem,
            this.sToolStripMenuItem,
            this.syradeEDatiToolStripMenuItem,
            this.terrenoToolStripMenuItem});
            this.stileToolStripMenuItem.Name = "stileToolStripMenuItem";
            this.stileToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.stileToolStripMenuItem.Text = "Visualizzazione";
            // 
            // datiToolStripMenuItem
            // 
            this.datiToolStripMenuItem.Name = "datiToolStripMenuItem";
            this.datiToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.datiToolStripMenuItem.Text = "Dati";
            this.datiToolStripMenuItem.Click += new System.EventHandler(this.datiToolStripMenuItem_Click);
            // 
            // notteToolStripMenuItem
            // 
            this.notteToolStripMenuItem.Name = "notteToolStripMenuItem";
            this.notteToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.notteToolStripMenuItem.Text = "Notte";
            this.notteToolStripMenuItem.Click += new System.EventHandler(this.notteToolStripMenuItem_Click);
            // 
            // politicaToolStripMenuItem
            // 
            this.politicaToolStripMenuItem.Name = "politicaToolStripMenuItem";
            this.politicaToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.politicaToolStripMenuItem.Text = "Politica";
            this.politicaToolStripMenuItem.Click += new System.EventHandler(this.politicaToolStripMenuItem_Click);
            // 
            // sToolStripMenuItem
            // 
            this.sToolStripMenuItem.Name = "sToolStripMenuItem";
            this.sToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.sToolStripMenuItem.Text = "Strade";
            this.sToolStripMenuItem.Click += new System.EventHandler(this.sToolStripMenuItem_Click);
            // 
            // syradeEDatiToolStripMenuItem
            // 
            this.syradeEDatiToolStripMenuItem.Name = "syradeEDatiToolStripMenuItem";
            this.syradeEDatiToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.syradeEDatiToolStripMenuItem.Text = "Strade e Dati";
            this.syradeEDatiToolStripMenuItem.Click += new System.EventHandler(this.syradeEDatiToolStripMenuItem_Click);
            // 
            // terrenoToolStripMenuItem
            // 
            this.terrenoToolStripMenuItem.Name = "terrenoToolStripMenuItem";
            this.terrenoToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
            this.terrenoToolStripMenuItem.Text = "Terreno";
            this.terrenoToolStripMenuItem.Click += new System.EventHandler(this.terrenoToolStripMenuItem_Click);
            // 
            // viasualizzaOnlineToolStripMenuItem
            // 
            this.viasualizzaOnlineToolStripMenuItem.Name = "viasualizzaOnlineToolStripMenuItem";
            this.viasualizzaOnlineToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.viasualizzaOnlineToolStripMenuItem.Text = "Viasualizza on-line";
            this.viasualizzaOnlineToolStripMenuItem.Click += new System.EventHandler(this.viasualizzaOnlineToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(168, 6);
            // 
            // misuraDistanzaToolStripMenuItem
            // 
            this.misuraDistanzaToolStripMenuItem.Name = "misuraDistanzaToolStripMenuItem";
            this.misuraDistanzaToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.misuraDistanzaToolStripMenuItem.Text = "Misura distanza";
            this.misuraDistanzaToolStripMenuItem.Click += new System.EventHandler(this.misuraDistanzaToolStripMenuItem_Click);
            // 
            // salvaToolStripMenuItem
            // 
            this.salvaToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.salvaComeToolStripMenuItem});
            this.salvaToolStripMenuItem.Name = "salvaToolStripMenuItem";
            this.salvaToolStripMenuItem.Size = new System.Drawing.Size(45, 20);
            this.salvaToolStripMenuItem.Text = "Salva";
            // 
            // salvaComeToolStripMenuItem
            // 
            this.salvaComeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.direzioneToolStripMenuItem,
            this.mappaToolStripMenuItem1,
            this.mappaEDirezioneToolStripMenuItem});
            this.salvaComeToolStripMenuItem.Name = "salvaComeToolStripMenuItem";
            this.salvaComeToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.salvaComeToolStripMenuItem.Text = "Salva come...";
            // 
            // direzioneToolStripMenuItem
            // 
            this.direzioneToolStripMenuItem.Name = "direzioneToolStripMenuItem";
            this.direzioneToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.direzioneToolStripMenuItem.Text = "Direzione";
            this.direzioneToolStripMenuItem.Click += new System.EventHandler(this.direzioneToolStripMenuItem_Click);
            // 
            // mappaToolStripMenuItem1
            // 
            this.mappaToolStripMenuItem1.Name = "mappaToolStripMenuItem1";
            this.mappaToolStripMenuItem1.Size = new System.Drawing.Size(173, 22);
            this.mappaToolStripMenuItem1.Text = "Mappa";
            this.mappaToolStripMenuItem1.Click += new System.EventHandler(this.mappaToolStripMenuItem1_Click);
            // 
            // mappaEDirezioneToolStripMenuItem
            // 
            this.mappaEDirezioneToolStripMenuItem.Name = "mappaEDirezioneToolStripMenuItem";
            this.mappaEDirezioneToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.mappaEDirezioneToolStripMenuItem.Text = "Mappa e Direzione";
            this.mappaEDirezioneToolStripMenuItem.Click += new System.EventHandler(this.mappaEDirezioneToolStripMenuItem_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "Direzione (*.htm)|*.htm|Mappa (*.htm)|*.htm|Mappa e direzione (*.htm)|*.htm";
            this.saveFileDialog1.Title = "Salva la mappa";
            // 
            // frmMapPoint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(866, 467);
            this.Controls.Add(this.Map);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMapPoint";
            this.Text = "Mappa";
            this.Load += new System.EventHandler(this.frmMapPoint_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMapPoint_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.Map)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public AxMapPoint.AxMappointControl Map;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mappaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem datiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem notteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem politicaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem syradeEDatiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terrenoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viasualizzaOnlineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem salvaToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem salvaComeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem direzioneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mappaToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem mappaEDirezioneToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem misuraDistanzaToolStripMenuItem;

    }
}
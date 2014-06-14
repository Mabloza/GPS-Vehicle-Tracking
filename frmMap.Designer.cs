namespace SMS
{
    partial class frmMap
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMap));
            this.satelliteMap = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // satelliteMap
            // 
            this.satelliteMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.satelliteMap.Location = new System.Drawing.Point(0, 0);
            this.satelliteMap.MinimumSize = new System.Drawing.Size(20, 20);
            this.satelliteMap.Name = "satelliteMap";
            this.satelliteMap.Size = new System.Drawing.Size(768, 544);
            this.satelliteMap.TabIndex = 7;
            // 
            // frmMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(767, 545);
            this.Controls.Add(this.satelliteMap);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmMap";
            this.Text = "Visualizzazione mappa";
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.WebBrowser satelliteMap;

    }
}
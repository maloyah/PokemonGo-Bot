namespace PokemonGo.RocketAPI.Console
{
    partial class LocationSelect
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private PokemonGo.RocketAPI.Console.LocationPanel locationPanel1;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LocationSelect));
            this.locationPanel1 = new PokemonGo.RocketAPI.Console.LocationPanel();
            this.SuspendLayout();
            // 
            // locationPanel1
            // 
            this.locationPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.locationPanel1.Location = new System.Drawing.Point(0, 0);
            this.locationPanel1.Name = "locationPanel1";
            this.locationPanel1.Size = new System.Drawing.Size(722, 481);
            this.locationPanel1.TabIndex = 0;
            // 
            // LocationSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(722, 481);
            this.Controls.Add(this.locationPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "LocationSelect";
            this.Text = "Location";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LocationSelect_FormClosing);
            this.ResumeLayout(false);

        }
        
		
        #endregion

    }
}
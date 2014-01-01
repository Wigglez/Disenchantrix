namespace Disenchantrix {
    partial class DisenchantrixGUI {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.disenchantrixPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // disenchantrixPropertyGrid
            // 
            this.disenchantrixPropertyGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.disenchantrixPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.disenchantrixPropertyGrid.Name = "disenchantrixPropertyGrid";
            this.disenchantrixPropertyGrid.Size = new System.Drawing.Size(301, 452);
            this.disenchantrixPropertyGrid.TabIndex = 0;
            this.disenchantrixPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            // 
            // DisenchantrixGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(301, 484);
            this.Controls.Add(this.disenchantrixPropertyGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(16, 38);
            this.Name = "DisenchantrixGUI";
            this.Text = "Disenchantrix";
            this.Load += new System.EventHandler(this.DisenchantrixGUI_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid disenchantrixPropertyGrid;
    }
}
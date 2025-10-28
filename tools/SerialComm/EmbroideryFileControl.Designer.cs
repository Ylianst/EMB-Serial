namespace SerialComm
{
    partial class EmbroideryFileControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblAttributes = new Label();
            pbPreview = new PictureBox();
            lblFileName = new Label();
            ((System.ComponentModel.ISupportInitialize)pbPreview).BeginInit();
            
            // Panel for attributes (top)
            lblAttributes.AutoSize = false;
            lblAttributes.Dock = DockStyle.Top;
            lblAttributes.Height = 40;
            lblAttributes.Padding = new Padding(5);
            lblAttributes.TextAlign = ContentAlignment.TopLeft;
            lblAttributes.Font = new Font("Segoe UI", 9F);
            lblAttributes.Name = "lblAttributes";
            
            // PictureBox for preview image (middle)
            pbPreview.Dock = DockStyle.Fill;
            pbPreview.SizeMode = PictureBoxSizeMode.CenterImage;
            pbPreview.BackColor = Color.White;
            pbPreview.Name = "pbPreview";
            pbPreview.TabStop = false;
            
            // Label for filename (bottom)
            lblFileName.AutoSize = false;
            lblFileName.Dock = DockStyle.Bottom;
            lblFileName.Height = 40;
            lblFileName.Padding = new Padding(5);
            lblFileName.TextAlign = ContentAlignment.MiddleCenter;
            lblFileName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFileName.AutoEllipsis = true;
            lblFileName.Name = "lblFileName";
            
            // Main control
            Controls.Add(pbPreview);
            Controls.Add(lblFileName);
            Controls.Add(lblAttributes);
            
            Name = "EmbroideryFileControl";
            BackColor = Color.White;
            BorderStyle = BorderStyle.Fixed3D;
            Padding = new Padding(2);
            Size = new Size(110, 140);
            
            ((System.ComponentModel.ISupportInitialize)pbPreview).EndInit();
        }

        private Label lblAttributes = null!;
        private PictureBox pbPreview = null!;
        private Label lblFileName = null!;
    }
}

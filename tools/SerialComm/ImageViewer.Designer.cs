namespace SerialComm
{
    partial class ImageViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageViewer));
            panel1 = new Panel();
            btnConvert = new Button();
            txtWidth = new TextBox();
            trackBarWidth = new TrackBar();
            lblWidth = new Label();
            txtHexData = new TextBox();
            lblHexData = new Label();
            pictureBox = new PictureBox();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(btnConvert);
            panel1.Controls.Add(txtWidth);
            panel1.Controls.Add(trackBarWidth);
            panel1.Controls.Add(lblWidth);
            panel1.Controls.Add(txtHexData);
            panel1.Controls.Add(lblHexData);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(384, 120);
            panel1.TabIndex = 0;
            // 
            // btnConvert
            // 
            btnConvert.Location = new Point(12, 82);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(110, 29);
            btnConvert.TabIndex = 4;
            btnConvert.Text = "Convert";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // txtWidth
            // 
            txtWidth.Location = new Point(322, 83);
            txtWidth.MaxLength = 3;
            txtWidth.Name = "txtWidth";
            txtWidth.ReadOnly = true;
            txtWidth.Size = new Size(50, 27);
            txtWidth.TabIndex = 3;
            txtWidth.Text = "72";
            txtWidth.TextAlign = HorizontalAlignment.Center;
            // 
            // trackBarWidth
            // 
            trackBarWidth.Location = new Point(227, 78);
            trackBarWidth.Maximum = 512;
            trackBarWidth.Minimum = 1;
            trackBarWidth.Name = "trackBarWidth";
            trackBarWidth.Size = new Size(89, 56);
            trackBarWidth.TabIndex = 5;
            trackBarWidth.Value = 72;
            trackBarWidth.Scroll += trackBarWidth_Scroll;
            // 
            // lblWidth
            // 
            lblWidth.AutoSize = true;
            lblWidth.Location = new Point(128, 86);
            lblWidth.Name = "lblWidth";
            lblWidth.Size = new Size(98, 20);
            lblWidth.TabIndex = 2;
            lblWidth.Text = "Width (pixel):";
            // 
            // txtHexData
            // 
            txtHexData.Location = new Point(111, 12);
            txtHexData.Multiline = true;
            txtHexData.Name = "txtHexData";
            txtHexData.PlaceholderText = "Paste HEX data here";
            txtHexData.ScrollBars = ScrollBars.Vertical;
            txtHexData.Size = new Size(261, 60);
            txtHexData.TabIndex = 1;
            txtHexData.Text = resources.GetString("txtHexData.Text");
            // 
            // lblHexData
            // 
            lblHexData.AutoSize = true;
            lblHexData.Location = new Point(12, 15);
            lblHexData.Name = "lblHexData";
            lblHexData.Size = new Size(76, 20);
            lblHexData.TabIndex = 0;
            lblHexData.Text = "HEX Data:";
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Location = new Point(0, 120);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(384, 304);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 1;
            pictureBox.TabStop = false;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 424);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(384, 26);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(50, 20);
            lblStatus.Text = "Ready";
            // 
            // ImageViewer
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(384, 450);
            Controls.Add(pictureBox);
            Controls.Add(statusStrip);
            Controls.Add(panel1);
            MinimumSize = new Size(400, 400);
            Name = "ImageViewer";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Image Viewer";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label lblHexData;
        private TextBox txtHexData;
        private Label lblWidth;
        private TextBox txtWidth;
        private TrackBar trackBarWidth;
        private Button btnConvert;
        private PictureBox pictureBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
    }
}

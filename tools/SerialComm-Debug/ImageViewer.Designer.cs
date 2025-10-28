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
            panel1 = new Panel();
            btnLoad = new Button();
            cmbStorageLocation = new ComboBox();
            lblStorageLocation = new Label();
            txtFileId = new TextBox();
            lblFileId = new Label();
            pictureBox = new PictureBox();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(btnLoad);
            panel1.Controls.Add(cmbStorageLocation);
            panel1.Controls.Add(lblStorageLocation);
            panel1.Controls.Add(txtFileId);
            panel1.Controls.Add(lblFileId);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(384, 80);
            panel1.TabIndex = 0;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(12, 46);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(110, 29);
            btnLoad.TabIndex = 4;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // cmbStorageLocation
            // 
            cmbStorageLocation.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStorageLocation.FormattingEnabled = true;
            cmbStorageLocation.Items.AddRange(new object[] { "Memory", "PC Card" });
            cmbStorageLocation.Location = new Point(240, 46);
            cmbStorageLocation.Name = "cmbStorageLocation";
            cmbStorageLocation.Size = new Size(132, 28);
            cmbStorageLocation.TabIndex = 3;
            cmbStorageLocation.SelectedIndex = 0;
            // 
            // lblStorageLocation
            // 
            lblStorageLocation.AutoSize = true;
            lblStorageLocation.Location = new Point(240, 15);
            lblStorageLocation.Name = "lblStorageLocation";
            lblStorageLocation.Size = new Size(119, 20);
            lblStorageLocation.TabIndex = 2;
            lblStorageLocation.Text = "Storage Location:";
            // 
            // txtFileId
            // 
            txtFileId.Location = new Point(111, 46);
            txtFileId.MaxLength = 5;
            txtFileId.Name = "txtFileId";
            txtFileId.Size = new Size(110, 27);
            txtFileId.TabIndex = 1;
            txtFileId.Text = "0";
            txtFileId.TextAlign = HorizontalAlignment.Center;
            // 
            // lblFileId
            // 
            lblFileId.AutoSize = true;
            lblFileId.Location = new Point(12, 15);
            lblFileId.Name = "lblFileId";
            lblFileId.Size = new Size(59, 20);
            lblFileId.TabIndex = 0;
            lblFileId.Text = "File ID:";
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Location = new Point(0, 80);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(384, 344);
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
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label lblFileId;
        private TextBox txtFileId;
        private Label lblStorageLocation;
        private ComboBox cmbStorageLocation;
        private Button btnLoad;
        private PictureBox pictureBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
    }
}

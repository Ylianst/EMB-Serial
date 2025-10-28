namespace SerialComm
{
    partial class MemoryDumpForm
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
            groupBoxSettings = new GroupBox();
            labelMode = new Label();
            comboBoxMode = new ComboBox();
            labelFilename = new Label();
            textBoxFilename = new TextBox();
            buttonBrowse = new Button();
            progressBar = new ProgressBar();
            buttonStart = new Button();
            buttonCancel = new Button();
            infoLabel = new Label();
            chipPictureBox = new PictureBox();
            statusLabel = new Label();
            groupBoxSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chipPictureBox).BeginInit();
            SuspendLayout();
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxSettings.Controls.Add(labelMode);
            groupBoxSettings.Controls.Add(comboBoxMode);
            groupBoxSettings.Controls.Add(labelFilename);
            groupBoxSettings.Controls.Add(textBoxFilename);
            groupBoxSettings.Controls.Add(buttonBrowse);
            groupBoxSettings.Location = new Point(15, 81);
            groupBoxSettings.Margin = new Padding(3, 4, 3, 4);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Padding = new Padding(3, 4, 3, 4);
            groupBoxSettings.Size = new Size(615, 117);
            groupBoxSettings.TabIndex = 1;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Download Settings";
            // 
            // labelMode
            // 
            labelMode.AutoSize = true;
            labelMode.Location = new Point(11, 33);
            labelMode.Name = "labelMode";
            labelMode.Size = new Size(54, 20);
            labelMode.TabIndex = 0;
            labelMode.Text = "Device";
            // 
            // comboBoxMode
            // 
            comboBoxMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMode.FormattingEnabled = true;
            comboBoxMode.Location = new Point(114, 29);
            comboBoxMode.Margin = new Padding(3, 4, 3, 4);
            comboBoxMode.Name = "comboBoxMode";
            comboBoxMode.Size = new Size(490, 28);
            comboBoxMode.TabIndex = 1;
            // 
            // labelFilename
            // 
            labelFilename.AutoSize = true;
            labelFilename.Location = new Point(11, 77);
            labelFilename.Name = "labelFilename";
            labelFilename.Size = new Size(83, 20);
            labelFilename.TabIndex = 2;
            labelFilename.Text = "Image Path";
            // 
            // textBoxFilename
            // 
            textBoxFilename.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxFilename.Location = new Point(114, 73);
            textBoxFilename.Margin = new Padding(3, 4, 3, 4);
            textBoxFilename.Name = "textBoxFilename";
            textBoxFilename.Size = new Size(385, 27);
            textBoxFilename.TabIndex = 3;
            // 
            // buttonBrowse
            // 
            buttonBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowse.Location = new Point(505, 73);
            buttonBrowse.Margin = new Padding(3, 4, 3, 4);
            buttonBrowse.Name = "buttonBrowse";
            buttonBrowse.Size = new Size(99, 31);
            buttonBrowse.TabIndex = 4;
            buttonBrowse.Text = "Browse...";
            buttonBrowse.UseVisualStyleBackColor = true;
            buttonBrowse.Click += buttonBrowse_Click;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(14, 209);
            progressBar.Margin = new Padding(3, 4, 3, 4);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(615, 31);
            progressBar.TabIndex = 0;
            // 
            // buttonStart
            // 
            buttonStart.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonStart.BackColor = Color.FromArgb(0, 120, 215);
            buttonStart.FlatStyle = FlatStyle.Flat;
            buttonStart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            buttonStart.ForeColor = Color.White;
            buttonStart.Location = new Point(450, 248);
            buttonStart.Margin = new Padding(3, 4, 3, 4);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(91, 40);
            buttonStart.TabIndex = 3;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = false;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            buttonCancel.Location = new Point(549, 248);
            buttonCancel.Margin = new Padding(3, 4, 3, 4);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(80, 40);
            buttonCancel.TabIndex = 4;
            buttonCancel.Text = "Close";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // infoLabel
            // 
            infoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            infoLabel.Location = new Point(15, 9);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new Size(546, 68);
            infoLabel.TabIndex = 5;
            infoLabel.Text = "Download entire memory from either the sewing machine or embroidery module to a .bin file. This creates a complete backup of the device memory.";
            // 
            // chipPictureBox
            // 
            chipPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chipPictureBox.Location = new Point(567, 9);
            chipPictureBox.Name = "chipPictureBox";
            chipPictureBox.Size = new Size(69, 68);
            chipPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            chipPictureBox.TabIndex = 6;
            chipPictureBox.TabStop = false;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Location = new Point(14, 259);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(418, 20);
            statusLabel.TabIndex = 7;
            // 
            // MemoryDumpForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(642, 304);
            Controls.Add(statusLabel);
            Controls.Add(chipPictureBox);
            Controls.Add(infoLabel);
            Controls.Add(progressBar);
            Controls.Add(buttonCancel);
            Controls.Add(buttonStart);
            Controls.Add(groupBoxSettings);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MemoryDumpForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Memory Download";
            Load += MemoryDumpForm_Load;
            groupBoxSettings.ResumeLayout(false);
            groupBoxSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)chipPictureBox).EndInit();
            ResumeLayout(false);
        }
        private System.Windows.Forms.GroupBox groupBoxSettings;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.Label labelFilename;
        private System.Windows.Forms.TextBox textBoxFilename;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonCancel;
        private Label infoLabel;
        private PictureBox chipPictureBox;
        private Label statusLabel;
    }
}

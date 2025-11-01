namespace EmbroideryCommunicator
{
    partial class EmbroideryFileDetailsDialog
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
            pbPreview = new PictureBox();
            lblFileId = new Label();
            lblFileName = new Label();
            lblAttributeDetails = new Label();
            btnOK = new Button();
            viewButton = new Button();
            downloadButton = new Button();
            ((System.ComponentModel.ISupportInitialize)pbPreview).BeginInit();
            SuspendLayout();
            // 
            // pbPreview
            // 
            pbPreview.BackColor = Color.White;
            pbPreview.BorderStyle = BorderStyle.Fixed3D;
            pbPreview.Location = new Point(14, 16);
            pbPreview.Margin = new Padding(3, 4, 3, 4);
            pbPreview.Name = "pbPreview";
            pbPreview.Size = new Size(171, 185);
            pbPreview.SizeMode = PictureBoxSizeMode.CenterImage;
            pbPreview.TabIndex = 0;
            pbPreview.TabStop = false;
            // 
            // lblFileId
            // 
            lblFileId.AutoSize = true;
            lblFileId.Location = new Point(191, 14);
            lblFileId.Name = "lblFileId";
            lblFileId.Size = new Size(66, 20);
            lblFileId.TabIndex = 1;
            lblFileId.Text = "File ID: 0";
            // 
            // lblFileName
            // 
            lblFileName.AutoSize = true;
            lblFileName.Location = new Point(191, 38);
            lblFileName.Name = "lblFileName";
            lblFileName.Size = new Size(99, 20);
            lblFileName.TabIndex = 2;
            lblFileName.Text = "Name: (none)";
            // 
            // lblAttributeDetails
            // 
            lblAttributeDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblAttributeDetails.Location = new Point(191, 62);
            lblAttributeDetails.Name = "lblAttributeDetails";
            lblAttributeDetails.Size = new Size(254, 139);
            lblAttributeDetails.TabIndex = 4;
            lblAttributeDetails.Text = "File Attributes:\r\n  • None";
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(339, 204);
            btnOK.Margin = new Padding(3, 4, 3, 4);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(106, 31);
            btnOK.TabIndex = 5;
            btnOK.Text = "Close";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // viewButton
            // 
            viewButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            viewButton.Location = new Point(115, 204);
            viewButton.Margin = new Padding(3, 4, 3, 4);
            viewButton.Name = "viewButton";
            viewButton.Size = new Size(106, 31);
            viewButton.TabIndex = 6;
            viewButton.Text = "View...";
            viewButton.UseVisualStyleBackColor = true;
            viewButton.Click += viewButton_Click;
            // 
            // downloadButton
            // 
            downloadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            downloadButton.Location = new Point(227, 205);
            downloadButton.Margin = new Padding(3, 4, 3, 4);
            downloadButton.Name = "downloadButton";
            downloadButton.Size = new Size(106, 31);
            downloadButton.TabIndex = 7;
            downloadButton.Text = "Download...";
            downloadButton.UseVisualStyleBackColor = true;
            downloadButton.Click += downloadButton_Click;
            // 
            // EmbroideryFileDetailsDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(457, 248);
            Controls.Add(downloadButton);
            Controls.Add(viewButton);
            Controls.Add(pbPreview);
            Controls.Add(lblFileId);
            Controls.Add(lblFileName);
            Controls.Add(lblAttributeDetails);
            Controls.Add(btnOK);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EmbroideryFileDetailsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Details";
            ((System.ComponentModel.ISupportInitialize)pbPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private PictureBox pbPreview = null!;
        private Label lblFileId = null!;
        private Label lblFileName = null!;
        private Label lblAttributeDetails = null!;
        private Button btnOK = null!;
        private Button viewButton;
        private Button downloadButton;
    }
}

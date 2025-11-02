namespace EmbroideryCommunicator
{
    partial class EmbroideryFileControl
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmbroideryFileControl));
            pbPreview = new PictureBox();
            contextMenuStrip = new ContextMenuStrip(components);
            detailsToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            downloadToolStripMenuItem = new ToolStripMenuItem();
            lblFileName = new Label();
            attributesPanel = new Panel();
            attributeImagesPanel = new Panel();
            userPictureBox = new PictureBox();
            alphabetPictureBox = new PictureBox();
            lockPictureBox = new PictureBox();
            toolStripMenuItem1 = new ToolStripSeparator();
            deleteToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            ((System.ComponentModel.ISupportInitialize)pbPreview).BeginInit();
            contextMenuStrip.SuspendLayout();
            attributesPanel.SuspendLayout();
            attributeImagesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)userPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)alphabetPictureBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lockPictureBox).BeginInit();
            SuspendLayout();
            // 
            // pbPreview
            // 
            pbPreview.BackColor = Color.White;
            pbPreview.ContextMenuStrip = contextMenuStrip;
            pbPreview.Dock = DockStyle.Fill;
            pbPreview.Location = new Point(2, 31);
            pbPreview.Name = "pbPreview";
            pbPreview.Size = new Size(106, 73);
            pbPreview.SizeMode = PictureBoxSizeMode.CenterImage;
            pbPreview.TabIndex = 0;
            pbPreview.TabStop = false;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.ImageScalingSize = new Size(20, 20);
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { detailsToolStripMenuItem, toolStripMenuItem2, viewToolStripMenuItem, downloadToolStripMenuItem, toolStripMenuItem1, deleteToolStripMenuItem });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(211, 140);
            contextMenuStrip.Opening += contextMenuStrip_Opening;
            // 
            // detailsToolStripMenuItem
            // 
            detailsToolStripMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            detailsToolStripMenuItem.Name = "detailsToolStripMenuItem";
            detailsToolStripMenuItem.Size = new Size(210, 24);
            detailsToolStripMenuItem.Text = "&Details...";
            detailsToolStripMenuItem.Click += detailsToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.Enabled = false;
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(210, 24);
            viewToolStripMenuItem.Text = "&View...";
            viewToolStripMenuItem.Click += viewToolStripMenuItem_Click;
            // 
            // downloadToolStripMenuItem
            // 
            downloadToolStripMenuItem.Enabled = false;
            downloadToolStripMenuItem.Name = "downloadToolStripMenuItem";
            downloadToolStripMenuItem.Size = new Size(210, 24);
            downloadToolStripMenuItem.Text = "D&ownload...";
            downloadToolStripMenuItem.Click += downloadToolStripMenuItem_Click;
            // 
            // lblFileName
            // 
            lblFileName.AutoEllipsis = true;
            lblFileName.ContextMenuStrip = contextMenuStrip;
            lblFileName.Dock = DockStyle.Bottom;
            lblFileName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFileName.Location = new Point(2, 104);
            lblFileName.Name = "lblFileName";
            lblFileName.Padding = new Padding(5);
            lblFileName.Size = new Size(106, 34);
            lblFileName.TabIndex = 1;
            lblFileName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // attributesPanel
            // 
            attributesPanel.ContextMenuStrip = contextMenuStrip;
            attributesPanel.Controls.Add(attributeImagesPanel);
            attributesPanel.Dock = DockStyle.Top;
            attributesPanel.Location = new Point(2, 2);
            attributesPanel.Name = "attributesPanel";
            attributesPanel.Size = new Size(106, 29);
            attributesPanel.TabIndex = 2;
            // 
            // attributeImagesPanel
            // 
            attributeImagesPanel.ContextMenuStrip = contextMenuStrip;
            attributeImagesPanel.Controls.Add(userPictureBox);
            attributeImagesPanel.Controls.Add(alphabetPictureBox);
            attributeImagesPanel.Controls.Add(lockPictureBox);
            attributeImagesPanel.Location = new Point(20, 4);
            attributeImagesPanel.Name = "attributeImagesPanel";
            attributeImagesPanel.Size = new Size(84, 20);
            attributeImagesPanel.TabIndex = 1;
            // 
            // userPictureBox
            // 
            userPictureBox.ContextMenuStrip = contextMenuStrip;
            userPictureBox.Dock = DockStyle.Right;
            userPictureBox.Image = (Image)resources.GetObject("userPictureBox.Image");
            userPictureBox.Location = new Point(12, 0);
            userPictureBox.Name = "userPictureBox";
            userPictureBox.Size = new Size(24, 20);
            userPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            userPictureBox.TabIndex = 2;
            userPictureBox.TabStop = false;
            userPictureBox.Visible = false;
            // 
            // alphabetPictureBox
            // 
            alphabetPictureBox.ContextMenuStrip = contextMenuStrip;
            alphabetPictureBox.Dock = DockStyle.Right;
            alphabetPictureBox.Image = (Image)resources.GetObject("alphabetPictureBox.Image");
            alphabetPictureBox.Location = new Point(36, 0);
            alphabetPictureBox.Name = "alphabetPictureBox";
            alphabetPictureBox.Size = new Size(24, 20);
            alphabetPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            alphabetPictureBox.TabIndex = 1;
            alphabetPictureBox.TabStop = false;
            alphabetPictureBox.Visible = false;
            // 
            // lockPictureBox
            // 
            lockPictureBox.ContextMenuStrip = contextMenuStrip;
            lockPictureBox.Dock = DockStyle.Right;
            lockPictureBox.Image = (Image)resources.GetObject("lockPictureBox.Image");
            lockPictureBox.Location = new Point(60, 0);
            lockPictureBox.Name = "lockPictureBox";
            lockPictureBox.Size = new Size(24, 20);
            lockPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            lockPictureBox.TabIndex = 0;
            lockPictureBox.TabStop = false;
            lockPictureBox.Visible = false;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(207, 6);
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Enabled = false;
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(210, 24);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(207, 6);
            // 
            // EmbroideryFileControl
            // 
            BackColor = Color.White;
            BorderStyle = BorderStyle.Fixed3D;
            Controls.Add(pbPreview);
            Controls.Add(attributesPanel);
            Controls.Add(lblFileName);
            Name = "EmbroideryFileControl";
            Padding = new Padding(2);
            Size = new Size(110, 140);
            ((System.ComponentModel.ISupportInitialize)pbPreview).EndInit();
            contextMenuStrip.ResumeLayout(false);
            attributesPanel.ResumeLayout(false);
            attributeImagesPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)userPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)alphabetPictureBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)lockPictureBox).EndInit();
            ResumeLayout(false);
        }
        private PictureBox pbPreview = null!;
        private Label lblFileName = null!;
        private Panel attributesPanel;
        private PictureBox lockPictureBox;
        private Panel attributeImagesPanel;
        private PictureBox userPictureBox;
        private PictureBox alphabetPictureBox;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem detailsToolStripMenuItem;
        private ToolStripMenuItem downloadToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem deleteToolStripMenuItem;
    }
}

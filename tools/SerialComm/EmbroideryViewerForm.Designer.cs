namespace EmbroideryCommunicator
{
    partial class EmbroideryViewerForm
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
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            closeToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            fileNameLabel = new ToolStripStatusLabel();
            stitchCountLabel = new ToolStripStatusLabel();
            zoomLabel = new ToolStripStatusLabel();
            dimensionsLabel = new ToolStripStatusLabel();
            renderPanel = new Panel();
            trackBar = new TrackBar();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 3, 0, 3);
            menuStrip.Size = new Size(1000, 30);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, closeToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(190, 26);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(190, 26);
            closeToolStripMenuItem.Text = "&Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(187, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(190, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { fileNameLabel, stitchCountLabel, zoomLabel, dimensionsLabel });
            statusStrip.Location = new Point(0, 626);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(1000, 26);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip1";
            // 
            // fileNameLabel
            // 
            fileNameLabel.Name = "fileNameLabel";
            fileNameLabel.Size = new Size(105, 20);
            fileNameLabel.Text = "No file loaded";
            // 
            // stitchCountLabel
            // 
            stitchCountLabel.Name = "stitchCountLabel";
            stitchCountLabel.Size = new Size(73, 20);
            stitchCountLabel.Text = "Stitches: -";
            // 
            // zoomLabel
            // 
            zoomLabel.Name = "zoomLabel";
            zoomLabel.Size = new Size(62, 20);
            zoomLabel.Text = "Zoom: -";
            // 
            // dimensionsLabel
            // 
            dimensionsLabel.Name = "dimensionsLabel";
            dimensionsLabel.Size = new Size(99, 20);
            dimensionsLabel.Text = "Dimensions: -";
            // 
            // renderPanel
            // 
            renderPanel.BackColor = Color.White;
            renderPanel.BorderStyle = BorderStyle.FixedSingle;
            renderPanel.Dock = DockStyle.Fill;
            renderPanel.Location = new Point(0, 30);
            renderPanel.Name = "renderPanel";
            renderPanel.Size = new Size(1000, 540);
            renderPanel.TabIndex = 2;
            renderPanel.Paint += renderPanel_Paint;
            renderPanel.MouseDown += renderPanel_MouseDown;
            renderPanel.MouseMove += renderPanel_MouseMove;
            renderPanel.MouseUp += renderPanel_MouseUp;
            renderPanel.MouseWheel += renderPanel_MouseWheel;
            // 
            // trackBar
            // 
            trackBar.Dock = DockStyle.Bottom;
            trackBar.Location = new Point(0, 570);
            trackBar.Maximum = 0;
            trackBar.Name = "trackBar";
            trackBar.Size = new Size(1000, 56);
            trackBar.TabIndex = 0;
            // 
            // EmbroideryViewerForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 652);
            Controls.Add(renderPanel);
            Controls.Add(trackBar);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            KeyPreview = true;
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(600, 400);
            Name = "EmbroideryViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Embroidery Viewer";
            KeyDown += EmbroideryViewerForm_KeyDown;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel fileNameLabel;
        private ToolStripStatusLabel stitchCountLabel;
        private ToolStripStatusLabel zoomLabel;
        private ToolStripStatusLabel dimensionsLabel;
        private Panel renderPanel;
        private TrackBar trackBar;
    }
}

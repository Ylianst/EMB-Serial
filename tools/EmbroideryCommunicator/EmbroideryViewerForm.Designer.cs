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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmbroideryViewerForm));
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            closeToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            previewImageToolStripMenuItem = new ToolStripMenuItem();
            jumpsToolStripMenuItem = new ToolStripMenuItem();
            stitchLinesToolStripMenuItem = new ToolStripMenuItem();
            stitchPointsToolStripMenuItem = new ToolStripMenuItem();
            zoomToolStripMenuItem = new ToolStripMenuItem();
            fitToWindowToolStripMenuItem = new ToolStripMenuItem();
            zoom50ToolStripMenuItem = new ToolStripMenuItem();
            zoom100ToolStripMenuItem = new ToolStripMenuItem();
            zoom200ToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            fileNameLabel = new ToolStripStatusLabel();
            stitchCountLabel = new ToolStripStatusLabel();
            zoomLabel = new ToolStripStatusLabel();
            dimensionsLabel = new ToolStripStatusLabel();
            renderPanel = new Panel();
            trackBar = new TrackBar();
            uploadToolStripMenuItem = new ToolStripMenuItem();
            toEmbroideryModuleToolStripMenuItem = new ToolStripMenuItem();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar).BeginInit();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, zoomToolStripMenuItem, uploadToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 3, 0, 3);
            menuStrip.Size = new Size(1000, 30);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, saveAsToolStripMenuItem, closeToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(224, 26);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveAsToolStripMenuItem.Size = new Size(224, 26);
            saveAsToolStripMenuItem.Text = "&Save As...";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(224, 26);
            closeToolStripMenuItem.Text = "&Close";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(221, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(224, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { previewImageToolStripMenuItem, jumpsToolStripMenuItem, stitchLinesToolStripMenuItem, stitchPointsToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(55, 24);
            viewToolStripMenuItem.Text = "&View";
            // 
            // previewImageToolStripMenuItem
            // 
            previewImageToolStripMenuItem.CheckOnClick = true;
            previewImageToolStripMenuItem.Name = "previewImageToolStripMenuItem";
            previewImageToolStripMenuItem.Size = new Size(224, 26);
            previewImageToolStripMenuItem.Text = "&Preview Image";
            previewImageToolStripMenuItem.Click += previewImageToolStripMenuItem_Click;
            // 
            // jumpsToolStripMenuItem
            // 
            jumpsToolStripMenuItem.Checked = true;
            jumpsToolStripMenuItem.CheckOnClick = true;
            jumpsToolStripMenuItem.CheckState = CheckState.Checked;
            jumpsToolStripMenuItem.Name = "jumpsToolStripMenuItem";
            jumpsToolStripMenuItem.Size = new Size(224, 26);
            jumpsToolStripMenuItem.Text = "&Jumps";
            jumpsToolStripMenuItem.Click += jumpsToolStripMenuItem_Click;
            // 
            // stitchLinesToolStripMenuItem
            // 
            stitchLinesToolStripMenuItem.Checked = true;
            stitchLinesToolStripMenuItem.CheckOnClick = true;
            stitchLinesToolStripMenuItem.CheckState = CheckState.Checked;
            stitchLinesToolStripMenuItem.Name = "stitchLinesToolStripMenuItem";
            stitchLinesToolStripMenuItem.Size = new Size(224, 26);
            stitchLinesToolStripMenuItem.Text = "Stitch &Lines";
            stitchLinesToolStripMenuItem.Click += stitchLinesToolStripMenuItem_Click;
            // 
            // stitchPointsToolStripMenuItem
            // 
            stitchPointsToolStripMenuItem.CheckOnClick = true;
            stitchPointsToolStripMenuItem.Name = "stitchPointsToolStripMenuItem";
            stitchPointsToolStripMenuItem.Size = new Size(224, 26);
            stitchPointsToolStripMenuItem.Text = "Stitch &Points";
            stitchPointsToolStripMenuItem.Click += stitchPointsToolStripMenuItem_Click;
            // 
            // zoomToolStripMenuItem
            // 
            zoomToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fitToWindowToolStripMenuItem, zoom50ToolStripMenuItem, zoom100ToolStripMenuItem, zoom200ToolStripMenuItem });
            zoomToolStripMenuItem.Name = "zoomToolStripMenuItem";
            zoomToolStripMenuItem.Size = new Size(63, 24);
            zoomToolStripMenuItem.Text = "&Zoom";
            // 
            // fitToWindowToolStripMenuItem
            // 
            fitToWindowToolStripMenuItem.Name = "fitToWindowToolStripMenuItem";
            fitToWindowToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.D0;
            fitToWindowToolStripMenuItem.Size = new Size(235, 26);
            fitToWindowToolStripMenuItem.Text = "&Fit to Window";
            fitToWindowToolStripMenuItem.Click += fitToWindowToolStripMenuItem_Click;
            // 
            // zoom50ToolStripMenuItem
            // 
            zoom50ToolStripMenuItem.Name = "zoom50ToolStripMenuItem";
            zoom50ToolStripMenuItem.Size = new Size(235, 26);
            zoom50ToolStripMenuItem.Text = "50%";
            zoom50ToolStripMenuItem.Click += zoom50ToolStripMenuItem_Click;
            // 
            // zoom100ToolStripMenuItem
            // 
            zoom100ToolStripMenuItem.Name = "zoom100ToolStripMenuItem";
            zoom100ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.D1;
            zoom100ToolStripMenuItem.Size = new Size(235, 26);
            zoom100ToolStripMenuItem.Text = "100%";
            zoom100ToolStripMenuItem.Click += zoom100ToolStripMenuItem_Click;
            // 
            // zoom200ToolStripMenuItem
            // 
            zoom200ToolStripMenuItem.Name = "zoom200ToolStripMenuItem";
            zoom200ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.D2;
            zoom200ToolStripMenuItem.Size = new Size(235, 26);
            zoom200ToolStripMenuItem.Text = "200%";
            zoom200ToolStripMenuItem.Click += zoom200ToolStripMenuItem_Click;
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
            renderPanel.AutoScroll = true;
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
            // uploadToolStripMenuItem
            // 
            uploadToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toEmbroideryModuleToolStripMenuItem });
            uploadToolStripMenuItem.Name = "uploadToolStripMenuItem";
            uploadToolStripMenuItem.Size = new Size(72, 24);
            uploadToolStripMenuItem.Text = "&Upload";
            // 
            // toEmbroideryModuleToolStripMenuItem
            // 
            toEmbroideryModuleToolStripMenuItem.Name = "toEmbroideryModuleToolStripMenuItem";
            toEmbroideryModuleToolStripMenuItem.Size = new Size(253, 26);
            toEmbroideryModuleToolStripMenuItem.Text = "To Embroidery Module...";
            toEmbroideryModuleToolStripMenuItem.Click += toEmbroideryModuleToolStripMenuItem_Click;
            // 
            // EmbroideryViewerForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 652);
            Controls.Add(renderPanel);
            Controls.Add(trackBar);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(600, 400);
            Name = "EmbroideryViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Embroidery Viewer";
            DragDrop += EmbroideryViewerForm_DragDrop;
            DragEnter += EmbroideryViewerForm_DragEnter;
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
        private ToolStripMenuItem saveAsToolStripMenuItem;
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
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem previewImageToolStripMenuItem;
        private ToolStripMenuItem jumpsToolStripMenuItem;
        private ToolStripMenuItem stitchLinesToolStripMenuItem;
        private ToolStripMenuItem stitchPointsToolStripMenuItem;
        private ToolStripMenuItem zoomToolStripMenuItem;
        private ToolStripMenuItem fitToWindowToolStripMenuItem;
        private ToolStripMenuItem zoom50ToolStripMenuItem;
        private ToolStripMenuItem zoom100ToolStripMenuItem;
        private ToolStripMenuItem zoom200ToolStripMenuItem;
        private ToolStripMenuItem uploadToolStripMenuItem;
        private ToolStripMenuItem toEmbroideryModuleToolStripMenuItem;
    }
}

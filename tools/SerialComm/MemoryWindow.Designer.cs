namespace EmbroideryCommunicator
{
    partial class MemoryWindow
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
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            refreshToolStripMenuItem = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            autoSumToolStripMenuItem = new ToolStripMenuItem();
            btnLoad = new Button();
            txtLength = new TextBox();
            lblLength = new Label();
            txtAddress = new TextBox();
            lblAddress = new Label();
            txtHexView = new TextBox();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            panel1 = new Panel();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(632, 28);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { refreshToolStripMenuItem, exportToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new Size(144, 26);
            refreshToolStripMenuItem.Text = "&Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(144, 26);
            exportToolStripMenuItem.Text = "&Export...";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(141, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(144, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { autoSumToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(55, 24);
            viewToolStripMenuItem.Text = "&View";
            // 
            // autoSumToolStripMenuItem
            // 
            autoSumToolStripMenuItem.CheckOnClick = true;
            autoSumToolStripMenuItem.Name = "autoSumToolStripMenuItem";
            autoSumToolStripMenuItem.Size = new Size(224, 26);
            autoSumToolStripMenuItem.Text = "&Auto-Sum";
            autoSumToolStripMenuItem.Click += autoSumToolStripMenuItem_Click;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Location = new Point(520, 2);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(100, 30);
            btnLoad.TabIndex = 4;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // txtLength
            // 
            txtLength.Location = new Point(339, 5);
            txtLength.Name = "txtLength";
            txtLength.Size = new Size(120, 27);
            txtLength.TabIndex = 3;
            // 
            // lblLength
            // 
            lblLength.AutoSize = true;
            lblLength.Location = new Point(242, 8);
            lblLength.Name = "lblLength";
            lblLength.Size = new Size(95, 20);
            lblLength.TabIndex = 2;
            lblLength.Text = "Length (dec):";
            // 
            // txtAddress
            // 
            txtAddress.Location = new Point(117, 5);
            txtAddress.Name = "txtAddress";
            txtAddress.Size = new Size(120, 27);
            txtAddress.TabIndex = 1;
            // 
            // lblAddress
            // 
            lblAddress.AutoSize = true;
            lblAddress.Location = new Point(12, 8);
            lblAddress.Name = "lblAddress";
            lblAddress.Size = new Size(102, 20);
            lblAddress.TabIndex = 0;
            lblAddress.Text = "Address (hex):";
            // 
            // txtHexView
            // 
            txtHexView.BackColor = Color.Black;
            txtHexView.Dock = DockStyle.Fill;
            txtHexView.Font = new Font("Consolas", 9F);
            txtHexView.ForeColor = Color.Lime;
            txtHexView.Location = new Point(0, 64);
            txtHexView.Multiline = true;
            txtHexView.Name = "txtHexView";
            txtHexView.ReadOnly = true;
            txtHexView.ScrollBars = ScrollBars.Vertical;
            txtHexView.Size = new Size(632, 413);
            txtHexView.TabIndex = 2;
            txtHexView.WordWrap = false;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar });
            statusStrip.Location = new Point(0, 477);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(632, 26);
            statusStrip.TabIndex = 3;
            statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(50, 20);
            lblStatus.Text = "Ready";
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 18);
            progressBar.Visible = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnLoad);
            panel1.Controls.Add(txtLength);
            panel1.Controls.Add(lblAddress);
            panel1.Controls.Add(lblLength);
            panel1.Controls.Add(txtAddress);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 28);
            panel1.Name = "panel1";
            panel1.Size = new Size(632, 36);
            panel1.TabIndex = 4;
            // 
            // MemoryWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(632, 503);
            Controls.Add(txtHexView);
            Controls.Add(panel1);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(650, 400);
            Name = "MemoryWindow";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Memory Viewer";
            FormClosing += MemoryWindow_FormClosing;
            Load += MemoryWindow_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem autoSumToolStripMenuItem;
        private TextBox txtAddress;
        private Label lblAddress;
        private Button btnLoad;
        private TextBox txtLength;
        private Label lblLength;
        private TextBox txtHexView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
        private Panel panel1;
    }
}

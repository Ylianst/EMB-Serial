namespace SerialComm
{
    partial class MainForm
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
            connectionToolStripMenuItem = new ToolStripMenuItem();
            selectCOMPortToolStripMenuItem = new ToolStripMenuItem();
            connectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            debugToolStripMenuItem = new ToolStripMenuItem();
            showDeveloperDebugToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabelConnection = new ToolStripStatusLabel();
            panelMain = new Panel();
            progressBarLoading = new ProgressBar();
            flowLayoutPanelFiles = new FlowLayoutPanel();
            mainPanel = new Panel();
            btnConnect = new Button();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            panelMain.SuspendLayout();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, debugToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 3, 0, 3);
            menuStrip.Size = new Size(759, 30);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { selectCOMPortToolStripMenuItem, connectToolStripMenuItem, disconnectToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(98, 24);
            connectionToolStripMenuItem.Text = "&Connection";
            // 
            // selectCOMPortToolStripMenuItem
            // 
            selectCOMPortToolStripMenuItem.Name = "selectCOMPortToolStripMenuItem";
            selectCOMPortToolStripMenuItem.Size = new Size(199, 26);
            selectCOMPortToolStripMenuItem.Text = "Select &COM Port";
            // 
            // connectToolStripMenuItem
            // 
            connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            connectToolStripMenuItem.Size = new Size(199, 26);
            connectToolStripMenuItem.Text = "&Connect";
            connectToolStripMenuItem.Click += connectToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Enabled = false;
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(199, 26);
            disconnectToolStripMenuItem.Text = "&Disconnect";
            disconnectToolStripMenuItem.Click += disconnectToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(196, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(199, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // debugToolStripMenuItem
            // 
            debugToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showDeveloperDebugToolStripMenuItem });
            debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            debugToolStripMenuItem.Size = new Size(68, 24);
            debugToolStripMenuItem.Text = "&Debug";
            // 
            // showDeveloperDebugToolStripMenuItem
            // 
            showDeveloperDebugToolStripMenuItem.Name = "showDeveloperDebugToolStripMenuItem";
            showDeveloperDebugToolStripMenuItem.Size = new Size(250, 26);
            showDeveloperDebugToolStripMenuItem.Text = "Show &Developer Debug";
            showDeveloperDebugToolStripMenuItem.Click += showDeveloperDebugToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripStatusLabelConnection });
            statusStrip.Location = new Point(0, 503);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(759, 26);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(583, 20);
            toolStripStatusLabel.Spring = true;
            toolStripStatusLabel.Text = "Ready";
            toolStripStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabelConnection
            // 
            toolStripStatusLabelConnection.AutoSize = false;
            toolStripStatusLabelConnection.ForeColor = Color.Red;
            toolStripStatusLabelConnection.Name = "toolStripStatusLabelConnection";
            toolStripStatusLabelConnection.Size = new Size(159, 20);
            toolStripStatusLabelConnection.Text = "Disconnected";
            toolStripStatusLabelConnection.TextAlign = ContentAlignment.MiddleRight;
            // 
            // panelMain
            // 
            panelMain.BorderStyle = BorderStyle.Fixed3D;
            panelMain.Controls.Add(progressBarLoading);
            panelMain.Controls.Add(flowLayoutPanelFiles);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 94);
            panelMain.Margin = new Padding(3, 4, 3, 4);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(4);
            panelMain.Size = new Size(759, 409);
            panelMain.TabIndex = 2;
            // 
            // progressBarLoading
            // 
            progressBarLoading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBarLoading.Location = new Point(279, 189);
            progressBarLoading.Name = "progressBarLoading";
            progressBarLoading.Size = new Size(200, 30);
            progressBarLoading.TabIndex = 1;
            progressBarLoading.Visible = false;
            // 
            // flowLayoutPanelFiles
            // 
            flowLayoutPanelFiles.AutoScroll = true;
            flowLayoutPanelFiles.BackColor = SystemColors.Control;
            flowLayoutPanelFiles.Dock = DockStyle.Fill;
            flowLayoutPanelFiles.Location = new Point(4, 4);
            flowLayoutPanelFiles.Margin = new Padding(0);
            flowLayoutPanelFiles.Name = "flowLayoutPanelFiles";
            flowLayoutPanelFiles.Size = new Size(747, 397);
            flowLayoutPanelFiles.TabIndex = 0;
            // 
            // mainPanel
            // 
            mainPanel.BackColor = SystemColors.ControlDark;
            mainPanel.Controls.Add(btnConnect);
            mainPanel.Dock = DockStyle.Top;
            mainPanel.Location = new Point(0, 30);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(759, 64);
            mainPanel.TabIndex = 3;
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnect.BackColor = SystemColors.Control;
            btnConnect.Font = new Font("Segoe UI", 10F);
            btnConnect.Location = new Point(634, 6);
            btnConnect.Margin = new Padding(3, 4, 3, 4);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(114, 53);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(759, 529);
            Controls.Add(panelMain);
            Controls.Add(mainPanel);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(777, 576);
            Name = "MainForm";
            Text = "Embroidery Serial Communicator";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            panelMain.ResumeLayout(false);
            mainPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip menuStrip;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem selectCOMPortToolStripMenuItem;
        private ToolStripMenuItem connectToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem debugToolStripMenuItem;
        private ToolStripMenuItem showDeveloperDebugToolStripMenuItem;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel;
        private ToolStripStatusLabel toolStripStatusLabelConnection;
        private Panel panelMain;
        private Panel mainPanel;
        private Button btnConnect;
        private FlowLayoutPanel flowLayoutPanelFiles;
        private ProgressBar progressBarLoading;
    }
}

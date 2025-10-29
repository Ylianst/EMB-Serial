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
            ListViewGroup listViewGroup1 = new ListViewGroup("Sewing Machine", HorizontalAlignment.Left);
            ListViewGroup listViewGroup2 = new ListViewGroup("Embroidery Module", HorizontalAlignment.Left);
            ListViewGroup listViewGroup3 = new ListViewGroup("PC Card", HorizontalAlignment.Left);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            menuStrip = new MenuStrip();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            selectCOMPortToolStripMenuItem = new ToolStripMenuItem();
            connectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            iconCacheToolStripMenuItem = new ToolStripMenuItem();
            iconCacheNoneToolStripMenuItem = new ToolStripMenuItem();
            iconCacheNormalToolStripMenuItem = new ToolStripMenuItem();
            iconCacheFastToolStripMenuItem = new ToolStripMenuItem();
            debugToolStripMenuItem = new ToolStripMenuItem();
            showDeveloperDebugToolStripMenuItem = new ToolStripMenuItem();
            downloadMemoryDumpToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripProgressBar = new ToolStripProgressBar();
            toolStripStatusLabelConnection = new ToolStripStatusLabel();
            panelMain = new Panel();
            mainTabControl = new TabControl();
            generalTabPage = new TabPage();
            machineInfoListView = new ListView();
            nameColumnHeader = new ColumnHeader();
            valueColumnHeader = new ColumnHeader();
            machineImageStatePanel = new Panel();
            pictureBox1 = new PictureBox();
            embroideryTabPage = new TabPage();
            flowLayoutPanelFiles = new FlowLayoutPanel();
            mainPanel = new Panel();
            btnConnect = new Button();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            panelMain.SuspendLayout();
            mainTabControl.SuspendLayout();
            generalTabPage.SuspendLayout();
            machineImageStatePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            embroideryTabPage.SuspendLayout();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, optionsToolStripMenuItem, debugToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 3, 0, 3);
            menuStrip.Size = new Size(843, 30);
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
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { iconCacheToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(75, 24);
            optionsToolStripMenuItem.Text = "&Options";
            // 
            // iconCacheToolStripMenuItem
            // 
            iconCacheToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { iconCacheNoneToolStripMenuItem, iconCacheNormalToolStripMenuItem, iconCacheFastToolStripMenuItem });
            iconCacheToolStripMenuItem.Name = "iconCacheToolStripMenuItem";
            iconCacheToolStripMenuItem.Size = new Size(164, 26);
            iconCacheToolStripMenuItem.Text = "Icon Cache";
            // 
            // iconCacheNoneToolStripMenuItem
            // 
            iconCacheNoneToolStripMenuItem.Name = "iconCacheNoneToolStripMenuItem";
            iconCacheNoneToolStripMenuItem.Size = new Size(142, 26);
            iconCacheNoneToolStripMenuItem.Text = "None";
            iconCacheNoneToolStripMenuItem.Click += IconCacheNone_Click;
            // 
            // iconCacheNormalToolStripMenuItem
            // 
            iconCacheNormalToolStripMenuItem.Checked = true;
            iconCacheNormalToolStripMenuItem.CheckState = CheckState.Checked;
            iconCacheNormalToolStripMenuItem.Name = "iconCacheNormalToolStripMenuItem";
            iconCacheNormalToolStripMenuItem.Size = new Size(142, 26);
            iconCacheNormalToolStripMenuItem.Text = "Normal";
            iconCacheNormalToolStripMenuItem.Click += IconCacheNormal_Click;
            // 
            // iconCacheFastToolStripMenuItem
            // 
            iconCacheFastToolStripMenuItem.Name = "iconCacheFastToolStripMenuItem";
            iconCacheFastToolStripMenuItem.Size = new Size(142, 26);
            iconCacheFastToolStripMenuItem.Text = "Fast";
            iconCacheFastToolStripMenuItem.Click += IconCacheFast_Click;
            // 
            // debugToolStripMenuItem
            // 
            debugToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showDeveloperDebugToolStripMenuItem, downloadMemoryDumpToolStripMenuItem });
            debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            debugToolStripMenuItem.Size = new Size(68, 24);
            debugToolStripMenuItem.Text = "&Debug";
            // 
            // showDeveloperDebugToolStripMenuItem
            // 
            showDeveloperDebugToolStripMenuItem.Name = "showDeveloperDebugToolStripMenuItem";
            showDeveloperDebugToolStripMenuItem.Size = new Size(219, 26);
            showDeveloperDebugToolStripMenuItem.Text = "&Developer Debug...";
            showDeveloperDebugToolStripMenuItem.Click += showDeveloperDebugToolStripMenuItem_Click;
            // 
            // downloadMemoryDumpToolStripMenuItem
            // 
            downloadMemoryDumpToolStripMenuItem.Name = "downloadMemoryDumpToolStripMenuItem";
            downloadMemoryDumpToolStripMenuItem.Size = new Size(219, 26);
            downloadMemoryDumpToolStripMenuItem.Text = "&Memory Dump...";
            downloadMemoryDumpToolStripMenuItem.Click += downloadMemoryDumpToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripProgressBar, toolStripStatusLabelConnection });
            statusStrip.Location = new Point(0, 602);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(843, 26);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(667, 20);
            toolStripStatusLabel.Spring = true;
            toolStripStatusLabel.Text = "Ready";
            toolStripStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripProgressBar
            // 
            toolStripProgressBar.AutoSize = false;
            toolStripProgressBar.Name = "toolStripProgressBar";
            toolStripProgressBar.Size = new Size(100, 18);
            toolStripProgressBar.Visible = false;
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
            panelMain.Controls.Add(mainTabControl);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 94);
            panelMain.Margin = new Padding(3, 4, 3, 4);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(4);
            panelMain.Size = new Size(843, 508);
            panelMain.TabIndex = 2;
            // 
            // mainTabControl
            // 
            mainTabControl.Controls.Add(generalTabPage);
            mainTabControl.Controls.Add(embroideryTabPage);
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Location = new Point(4, 4);
            mainTabControl.Name = "mainTabControl";
            mainTabControl.SelectedIndex = 0;
            mainTabControl.Size = new Size(831, 496);
            mainTabControl.TabIndex = 1;
            // 
            // generalTabPage
            // 
            generalTabPage.Controls.Add(machineInfoListView);
            generalTabPage.Controls.Add(machineImageStatePanel);
            generalTabPage.Location = new Point(4, 29);
            generalTabPage.Name = "generalTabPage";
            generalTabPage.Padding = new Padding(3);
            generalTabPage.Size = new Size(823, 463);
            generalTabPage.TabIndex = 0;
            generalTabPage.Text = "General";
            generalTabPage.UseVisualStyleBackColor = true;
            // 
            // machineInfoListView
            // 
            machineInfoListView.Columns.AddRange(new ColumnHeader[] { nameColumnHeader, valueColumnHeader });
            machineInfoListView.Dock = DockStyle.Fill;
            machineInfoListView.FullRowSelect = true;
            machineInfoListView.GridLines = true;
            listViewGroup1.Header = "Sewing Machine";
            listViewGroup1.Name = "sewingMachineListViewGroup";
            listViewGroup2.Header = "Embroidery Module";
            listViewGroup2.Name = "embroideryModuleListViewGroup1";
            listViewGroup3.Header = "PC Card";
            listViewGroup3.Name = "pcCardListViewGroup";
            machineInfoListView.Groups.AddRange(new ListViewGroup[] { listViewGroup1, listViewGroup2, listViewGroup3 });
            machineInfoListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            machineInfoListView.Location = new Point(292, 3);
            machineInfoListView.Name = "machineInfoListView";
            machineInfoListView.Size = new Size(528, 457);
            machineInfoListView.TabIndex = 1;
            machineInfoListView.UseCompatibleStateImageBehavior = false;
            machineInfoListView.View = View.Details;
            // 
            // nameColumnHeader
            // 
            nameColumnHeader.Text = "Name";
            nameColumnHeader.Width = 200;
            // 
            // valueColumnHeader
            // 
            valueColumnHeader.Text = "Value";
            valueColumnHeader.Width = 300;
            // 
            // machineImageStatePanel
            // 
            machineImageStatePanel.Controls.Add(pictureBox1);
            machineImageStatePanel.Dock = DockStyle.Left;
            machineImageStatePanel.Location = new Point(3, 3);
            machineImageStatePanel.Margin = new Padding(0);
            machineImageStatePanel.Name = "machineImageStatePanel";
            machineImageStatePanel.Size = new Size(289, 457);
            machineImageStatePanel.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(-4, -3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(293, 353);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // embroideryTabPage
            // 
            embroideryTabPage.Controls.Add(flowLayoutPanelFiles);
            embroideryTabPage.Location = new Point(4, 29);
            embroideryTabPage.Name = "embroideryTabPage";
            embroideryTabPage.Padding = new Padding(3);
            embroideryTabPage.Size = new Size(823, 463);
            embroideryTabPage.TabIndex = 1;
            embroideryTabPage.Text = "Embroidery Module";
            embroideryTabPage.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanelFiles
            // 
            flowLayoutPanelFiles.AutoScroll = true;
            flowLayoutPanelFiles.BackColor = SystemColors.Control;
            flowLayoutPanelFiles.Dock = DockStyle.Fill;
            flowLayoutPanelFiles.Location = new Point(3, 3);
            flowLayoutPanelFiles.Margin = new Padding(0);
            flowLayoutPanelFiles.Name = "flowLayoutPanelFiles";
            flowLayoutPanelFiles.Size = new Size(817, 457);
            flowLayoutPanelFiles.TabIndex = 0;
            // 
            // mainPanel
            // 
            mainPanel.BackColor = SystemColors.ControlDark;
            mainPanel.Controls.Add(btnConnect);
            mainPanel.Dock = DockStyle.Top;
            mainPanel.Location = new Point(0, 30);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(843, 64);
            mainPanel.TabIndex = 3;
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConnect.BackColor = SystemColors.Control;
            btnConnect.Font = new Font("Segoe UI", 10F);
            btnConnect.Location = new Point(718, 6);
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
            ClientSize = new Size(843, 628);
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
            mainTabControl.ResumeLayout(false);
            generalTabPage.ResumeLayout(false);
            machineImageStatePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            embroideryTabPage.ResumeLayout(false);
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
        private ToolStripMenuItem downloadMemoryDumpToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem iconCacheToolStripMenuItem;
        private ToolStripMenuItem iconCacheNoneToolStripMenuItem;
        private ToolStripMenuItem iconCacheNormalToolStripMenuItem;
        private ToolStripMenuItem iconCacheFastToolStripMenuItem;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel;
        private ToolStripProgressBar toolStripProgressBar;
        private ToolStripStatusLabel toolStripStatusLabelConnection;
        private Panel panelMain;
        private Panel mainPanel;
        private Button btnConnect;
        private FlowLayoutPanel flowLayoutPanelFiles;
        private TabControl mainTabControl;
        private TabPage generalTabPage;
        private TabPage embroideryTabPage;
        private ListView machineInfoListView;
        private Panel machineImageStatePanel;
        private PictureBox pictureBox1;
        private ColumnHeader nameColumnHeader;
        private ColumnHeader valueColumnHeader;
    }
}

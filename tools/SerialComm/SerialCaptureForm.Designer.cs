namespace EmbroideryCommunicator
{
    partial class SerialCaptureForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip = new MenuStrip();
            menuPorts = new ToolStripMenuItem();
            menuSoftwarePort = new ToolStripMenuItem();
            menuMachinePort = new ToolStripMenuItem();
            menuSpeed = new ToolStripMenuItem();
            menuOptions = new ToolStripMenuItem();
            menuDebugMode = new ToolStripMenuItem();
            menuShowErrors = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            menuSetLogFile = new ToolStripMenuItem();
            btnBrowseLog = new Button();
            txtLogFile = new TextBox();
            lblLogFile = new Label();
            chkShowErrors = new CheckBox();
            chkDebugMode = new CheckBox();
            btnStartStop = new Button();
            txtCapture = new TextBox();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            topPanel = new Panel();
            menuStrip.SuspendLayout();
            statusStrip1.SuspendLayout();
            topPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { menuPorts, menuSpeed, menuOptions });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(6, 3, 0, 3);
            menuStrip.Size = new Size(859, 30);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // menuPorts
            // 
            menuPorts.DropDownItems.AddRange(new ToolStripItem[] { menuSoftwarePort, menuMachinePort });
            menuPorts.Name = "menuPorts";
            menuPorts.Size = new Size(55, 24);
            menuPorts.Text = "Ports";
            // 
            // menuSoftwarePort
            // 
            menuSoftwarePort.Name = "menuSoftwarePort";
            menuSoftwarePort.Size = new Size(224, 26);
            menuSoftwarePort.Text = "Software Port";
            // 
            // menuMachinePort
            // 
            menuMachinePort.Name = "menuMachinePort";
            menuMachinePort.Size = new Size(224, 26);
            menuMachinePort.Text = "Machine Port";
            // 
            // menuSpeed
            // 
            menuSpeed.Name = "menuSpeed";
            menuSpeed.Size = new Size(65, 24);
            menuSpeed.Text = "Speed";
            // 
            // menuOptions
            // 
            menuOptions.DropDownItems.AddRange(new ToolStripItem[] { menuDebugMode, menuShowErrors, toolStripSeparator1, menuSetLogFile });
            menuOptions.Name = "menuOptions";
            menuOptions.Size = new Size(75, 24);
            menuOptions.Text = "Options";
            // 
            // menuDebugMode
            // 
            menuDebugMode.CheckOnClick = true;
            menuDebugMode.Name = "menuDebugMode";
            menuDebugMode.Size = new Size(224, 26);
            menuDebugMode.Text = "Debug Mode";
            menuDebugMode.CheckedChanged += menuDebugMode_CheckedChanged;
            // 
            // menuShowErrors
            // 
            menuShowErrors.CheckOnClick = true;
            menuShowErrors.Name = "menuShowErrors";
            menuShowErrors.Size = new Size(224, 26);
            menuShowErrors.Text = "Show Errors";
            menuShowErrors.CheckedChanged += menuShowErrors_CheckedChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(221, 6);
            // 
            // menuSetLogFile
            // 
            menuSetLogFile.Name = "menuSetLogFile";
            menuSetLogFile.Size = new Size(224, 26);
            menuSetLogFile.Text = "Set Log File...";
            menuSetLogFile.Click += menuSetLogFile_Click;
            // 
            // btnBrowseLog
            // 
            btnBrowseLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseLog.Location = new Point(762, 14);
            btnBrowseLog.Name = "btnBrowseLog";
            btnBrowseLog.Size = new Size(85, 29);
            btnBrowseLog.TabIndex = 5;
            btnBrowseLog.Text = "Browse...";
            btnBrowseLog.UseVisualStyleBackColor = true;
            btnBrowseLog.Click += btnBrowseLog_Click;
            // 
            // txtLogFile
            // 
            txtLogFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtLogFile.Location = new Point(84, 15);
            txtLogFile.Name = "txtLogFile";
            txtLogFile.Size = new Size(662, 27);
            txtLogFile.TabIndex = 4;
            txtLogFile.Text = "capture.log";
            // 
            // lblLogFile
            // 
            lblLogFile.AutoSize = true;
            lblLogFile.Location = new Point(14, 18);
            lblLogFile.Name = "lblLogFile";
            lblLogFile.Size = new Size(64, 20);
            lblLogFile.TabIndex = 3;
            lblLogFile.Text = "Log File:";
            // 
            // chkShowErrors
            // 
            chkShowErrors.AutoSize = true;
            chkShowErrors.Location = new Point(194, 56);
            chkShowErrors.Name = "chkShowErrors";
            chkShowErrors.Size = new Size(109, 24);
            chkShowErrors.TabIndex = 2;
            chkShowErrors.Text = "Show Errors";
            chkShowErrors.UseVisualStyleBackColor = true;
            chkShowErrors.CheckedChanged += chkShowErrors_CheckedChanged;
            // 
            // chkDebugMode
            // 
            chkDebugMode.AutoSize = true;
            chkDebugMode.Location = new Point(14, 56);
            chkDebugMode.Name = "chkDebugMode";
            chkDebugMode.Size = new Size(119, 24);
            chkDebugMode.TabIndex = 1;
            chkDebugMode.Text = "Debug Mode";
            chkDebugMode.UseVisualStyleBackColor = true;
            chkDebugMode.CheckedChanged += chkDebugMode_CheckedChanged;
            // 
            // btnStartStop
            // 
            btnStartStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStartStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStartStop.Location = new Point(672, 49);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(175, 35);
            btnStartStop.TabIndex = 2;
            btnStartStop.Text = "Start Capture";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // txtCapture
            // 
            txtCapture.BackColor = Color.Black;
            txtCapture.Dock = DockStyle.Fill;
            txtCapture.Font = new Font("Consolas", 9F);
            txtCapture.ForeColor = Color.Lime;
            txtCapture.Location = new Point(0, 124);
            txtCapture.Multiline = true;
            txtCapture.Name = "txtCapture";
            txtCapture.ReadOnly = true;
            txtCapture.ScrollBars = ScrollBars.Vertical;
            txtCapture.Size = new Size(859, 378);
            txtCapture.TabIndex = 3;
            txtCapture.WordWrap = false;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 502);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(859, 22);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(0, 16);
            // 
            // topPanel
            // 
            topPanel.Controls.Add(btnBrowseLog);
            topPanel.Controls.Add(lblLogFile);
            topPanel.Controls.Add(txtLogFile);
            topPanel.Controls.Add(btnStartStop);
            topPanel.Controls.Add(chkDebugMode);
            topPanel.Controls.Add(chkShowErrors);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 30);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(859, 94);
            topPanel.TabIndex = 6;
            // 
            // SerialCaptureForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(859, 524);
            Controls.Add(txtCapture);
            Controls.Add(topPanel);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(800, 500);
            Name = "SerialCaptureForm";
            Text = "Serial Capture";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem menuPorts;
        private ToolStripMenuItem menuSoftwarePort;
        private ToolStripMenuItem menuMachinePort;
        private ToolStripMenuItem menuSpeed;
        private ToolStripMenuItem menuOptions;
        private ToolStripMenuItem menuDebugMode;
        private ToolStripMenuItem menuShowErrors;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem menuSetLogFile;
        private Button btnBrowseLog;
        private TextBox txtLogFile;
        private Label lblLogFile;
        private CheckBox chkShowErrors;
        private CheckBox chkDebugMode;
        private Button btnStartStop;
        private TextBox txtCapture;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel;
        private Panel topPanel;
    }
}

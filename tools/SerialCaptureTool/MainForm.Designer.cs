namespace SerialCaptureTool
{
    partial class MainForm
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
            grpSettings = new GroupBox();
            btnBrowseLog = new Button();
            txtLogFile = new TextBox();
            lblLogFile = new Label();
            chkShowErrors = new CheckBox();
            chkDebugMode = new CheckBox();
            btnStartStop = new Button();
            txtCapture = new TextBox();
            lblStatus = new Label();
            menuStrip.SuspendLayout();
            grpSettings.SuspendLayout();
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
            menuSpeed.Size = new Size(62, 24);
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
            menuDebugMode.Size = new Size(226, 26);
            menuDebugMode.Text = "Debug Mode";
            menuDebugMode.CheckedChanged += menuDebugMode_CheckedChanged;
            // 
            // menuShowErrors
            // 
            menuShowErrors.CheckOnClick = true;
            menuShowErrors.Name = "menuShowErrors";
            menuShowErrors.Size = new Size(226, 26);
            menuShowErrors.Text = "Show Errors";
            menuShowErrors.CheckedChanged += menuShowErrors_CheckedChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(223, 6);
            // 
            // menuSetLogFile
            // 
            menuSetLogFile.Name = "menuSetLogFile";
            menuSetLogFile.Size = new Size(226, 26);
            menuSetLogFile.Text = "Set Log File...";
            menuSetLogFile.Click += menuSetLogFile_Click;
            // 
            // grpSettings
            // 
            grpSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpSettings.Controls.Add(btnBrowseLog);
            grpSettings.Controls.Add(txtLogFile);
            grpSettings.Controls.Add(lblLogFile);
            grpSettings.Controls.Add(chkShowErrors);
            grpSettings.Controls.Add(chkDebugMode);
            grpSettings.Location = new Point(12, 37);
            grpSettings.Name = "grpSettings";
            grpSettings.Size = new Size(835, 100);
            grpSettings.TabIndex = 1;
            grpSettings.TabStop = false;
            grpSettings.Text = "Settings";
            // 
            // btnBrowseLog
            // 
            btnBrowseLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseLog.Location = new Point(735, 30);
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
            txtLogFile.Location = new Point(90, 31);
            txtLogFile.Name = "txtLogFile";
            txtLogFile.Size = new Size(635, 27);
            txtLogFile.TabIndex = 4;
            txtLogFile.Text = "capture.log";
            // 
            // lblLogFile
            // 
            lblLogFile.AutoSize = true;
            lblLogFile.Location = new Point(20, 34);
            lblLogFile.Name = "lblLogFile";
            lblLogFile.Size = new Size(64, 20);
            lblLogFile.TabIndex = 3;
            lblLogFile.Text = "Log File:";
            // 
            // chkShowErrors
            // 
            chkShowErrors.AutoSize = true;
            chkShowErrors.Location = new Point(200, 65);
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
            chkDebugMode.Location = new Point(20, 65);
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
            btnStartStop.Location = new Point(672, 143);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(175, 40);
            btnStartStop.TabIndex = 2;
            btnStartStop.Text = "Start Capture";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // txtCapture
            // 
            txtCapture.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtCapture.BackColor = Color.Black;
            txtCapture.Font = new Font("Consolas", 9F);
            txtCapture.ForeColor = Color.Lime;
            txtCapture.Location = new Point(12, 189);
            txtCapture.Multiline = true;
            txtCapture.Name = "txtCapture";
            txtCapture.ReadOnly = true;
            txtCapture.ScrollBars = ScrollBars.Vertical;
            txtCapture.Size = new Size(835, 323);
            txtCapture.TabIndex = 3;
            txtCapture.WordWrap = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(12, 153);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(53, 20);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Status";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(859, 524);
            Controls.Add(lblStatus);
            Controls.Add(txtCapture);
            Controls.Add(btnStartStop);
            Controls.Add(grpSettings);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(800, 500);
            Name = "MainForm";
            Text = "Serial Capture Tool";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            grpSettings.ResumeLayout(false);
            grpSettings.PerformLayout();
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
        private GroupBox grpSettings;
        private Button btnBrowseLog;
        private TextBox txtLogFile;
        private Label lblLogFile;
        private CheckBox chkShowErrors;
        private CheckBox chkDebugMode;
        private Button btnStartStop;
        private TextBox txtCapture;
        private Label lblStatus;
    }
}

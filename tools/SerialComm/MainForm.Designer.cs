namespace SerialComm
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
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            showSerialTrafficToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            clearOutputToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            selectCOMPortToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            connectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            switchTo19200BaudToolStripMenuItem = new ToolStripMenuItem();
            switchTo57600BaudToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            autoSwitchTo57600BaudToolStripMenuItem = new ToolStripMenuItem();
            commandsToolStripMenuItem = new ToolStripMenuItem();
            readToolStripMenuItem = new ToolStripMenuItem();
            largeReadToolStripMenuItem = new ToolStripMenuItem();
            writeToolStripMenuItem = new ToolStripMenuItem();
            sumToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            memoryViewerToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabelConnection = new ToolStripStatusLabel();
            btnConnect = new Button();
            btnWrite = new Button();
            txtWriteData = new TextBox();
            label5 = new Label();
            txtWriteAddress = new TextBox();
            label4 = new Label();
            btnLargeRead = new Button();
            btnRead = new Button();
            txtReadAddress = new TextBox();
            label2 = new Label();
            txtLargeReadAddress = new TextBox();
            label3 = new Label();
            txtOutput = new TextBox();
            panel1 = new Panel();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, connectionToolStripMenuItem, commandsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(865, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showSerialTrafficToolStripMenuItem, toolStripSeparator2, clearOutputToolStripMenuItem, toolStripSeparator4, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // showSerialTrafficToolStripMenuItem
            // 
            showSerialTrafficToolStripMenuItem.CheckOnClick = true;
            showSerialTrafficToolStripMenuItem.Name = "showSerialTrafficToolStripMenuItem";
            showSerialTrafficToolStripMenuItem.Size = new Size(214, 26);
            showSerialTrafficToolStripMenuItem.Text = "&Show Serial Traffic";
            showSerialTrafficToolStripMenuItem.Click += showSerialTrafficToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(211, 6);
            // 
            // clearOutputToolStripMenuItem
            // 
            clearOutputToolStripMenuItem.Name = "clearOutputToolStripMenuItem";
            clearOutputToolStripMenuItem.Size = new Size(214, 26);
            clearOutputToolStripMenuItem.Text = "&Clear Output";
            clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(211, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(214, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { selectCOMPortToolStripMenuItem, toolStripSeparator5, connectToolStripMenuItem, disconnectToolStripMenuItem, toolStripSeparator1, switchTo19200BaudToolStripMenuItem, switchTo57600BaudToolStripMenuItem, toolStripMenuItem1, autoSwitchTo57600BaudToolStripMenuItem });
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(98, 24);
            connectionToolStripMenuItem.Text = "&Connection";
            // 
            // selectCOMPortToolStripMenuItem
            // 
            selectCOMPortToolStripMenuItem.Name = "selectCOMPortToolStripMenuItem";
            selectCOMPortToolStripMenuItem.Size = new Size(273, 26);
            selectCOMPortToolStripMenuItem.Text = "Select COM &Port";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(270, 6);
            // 
            // connectToolStripMenuItem
            // 
            connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            connectToolStripMenuItem.Size = new Size(273, 26);
            connectToolStripMenuItem.Text = "&Connect";
            connectToolStripMenuItem.Click += connectToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Enabled = false;
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(273, 26);
            disconnectToolStripMenuItem.Text = "&Disconnect";
            disconnectToolStripMenuItem.Click += disconnectToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(270, 6);
            // 
            // switchTo19200BaudToolStripMenuItem
            // 
            switchTo19200BaudToolStripMenuItem.Enabled = false;
            switchTo19200BaudToolStripMenuItem.Name = "switchTo19200BaudToolStripMenuItem";
            switchTo19200BaudToolStripMenuItem.Size = new Size(273, 26);
            switchTo19200BaudToolStripMenuItem.Text = "Switch to &19200 Baud";
            switchTo19200BaudToolStripMenuItem.Click += switchTo19200BaudToolStripMenuItem_Click;
            // 
            // switchTo57600BaudToolStripMenuItem
            // 
            switchTo57600BaudToolStripMenuItem.Enabled = false;
            switchTo57600BaudToolStripMenuItem.Name = "switchTo57600BaudToolStripMenuItem";
            switchTo57600BaudToolStripMenuItem.Size = new Size(273, 26);
            switchTo57600BaudToolStripMenuItem.Text = "Switch to &57600 Baud";
            switchTo57600BaudToolStripMenuItem.Click += switchTo57600BaudToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(270, 6);
            // 
            // autoSwitchTo57600BaudToolStripMenuItem
            // 
            autoSwitchTo57600BaudToolStripMenuItem.CheckOnClick = true;
            autoSwitchTo57600BaudToolStripMenuItem.Name = "autoSwitchTo57600BaudToolStripMenuItem";
            autoSwitchTo57600BaudToolStripMenuItem.Size = new Size(273, 26);
            autoSwitchTo57600BaudToolStripMenuItem.Text = "&Auto-Switch to 57600 Baud";
            autoSwitchTo57600BaudToolStripMenuItem.Click += autoSwitchTo57600BaudToolStripMenuItem_Click;
            // 
            // commandsToolStripMenuItem
            // 
            commandsToolStripMenuItem.Name = "commandsToolStripMenuItem";
            commandsToolStripMenuItem.Size = new Size(98, 24);
            commandsToolStripMenuItem.Text = "C&ommands";
            // 
            // sessionStartToolStripMenuItem
            // 
            sessionStartToolStripMenuItem = new ToolStripMenuItem();
            sessionStartToolStripMenuItem.Enabled = false;
            sessionStartToolStripMenuItem.Name = "sessionStartToolStripMenuItem";
            sessionStartToolStripMenuItem.Size = new Size(205, 26);
            sessionStartToolStripMenuItem.Text = "&Session Start";
            sessionStartToolStripMenuItem.Click += sessionStartToolStripMenuItem_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6 = new ToolStripSeparator();
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(202, 6);
            // 
            // commandsToolStripMenuItem items
            // 
            commandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { sessionStartToolStripMenuItem, toolStripSeparator6, readToolStripMenuItem, largeReadToolStripMenuItem, writeToolStripMenuItem, sumToolStripMenuItem, toolStripSeparator3, memoryViewerToolStripMenuItem });
            // 
            // readToolStripMenuItem
            // 
            readToolStripMenuItem.Enabled = false;
            readToolStripMenuItem.Name = "readToolStripMenuItem";
            readToolStripMenuItem.Size = new Size(205, 26);
            readToolStripMenuItem.Text = "&Read";
            readToolStripMenuItem.Click += readToolStripMenuItem_Click;
            // 
            // largeReadToolStripMenuItem
            // 
            largeReadToolStripMenuItem.Enabled = false;
            largeReadToolStripMenuItem.Name = "largeReadToolStripMenuItem";
            largeReadToolStripMenuItem.Size = new Size(205, 26);
            largeReadToolStripMenuItem.Text = "&Large Read";
            largeReadToolStripMenuItem.Click += largeReadToolStripMenuItem_Click;
            // 
            // writeToolStripMenuItem
            // 
            writeToolStripMenuItem.Enabled = false;
            writeToolStripMenuItem.Name = "writeToolStripMenuItem";
            writeToolStripMenuItem.Size = new Size(205, 26);
            writeToolStripMenuItem.Text = "&Write";
            writeToolStripMenuItem.Click += writeToolStripMenuItem_Click;
            // 
            // sumToolStripMenuItem
            // 
            sumToolStripMenuItem.Enabled = false;
            sumToolStripMenuItem.Name = "sumToolStripMenuItem";
            sumToolStripMenuItem.Size = new Size(205, 26);
            sumToolStripMenuItem.Text = "&Sum...";
            sumToolStripMenuItem.Click += sumToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(202, 6);
            // 
            // memoryViewerToolStripMenuItem
            // 
            memoryViewerToolStripMenuItem.Enabled = false;
            memoryViewerToolStripMenuItem.Name = "memoryViewerToolStripMenuItem";
            memoryViewerToolStripMenuItem.Size = new Size(205, 26);
            memoryViewerToolStripMenuItem.Text = "&Memory Viewer...";
            memoryViewerToolStripMenuItem.Click += memoryViewerToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(55, 24);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(133, 26);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripStatusLabelConnection });
            statusStrip1.Location = new Point(0, 580);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(865, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(50, 20);
            toolStripStatusLabel.Text = "Ready";
            // 
            // toolStripStatusLabelConnection
            // 
            toolStripStatusLabelConnection.Name = "toolStripStatusLabelConnection";
            toolStripStatusLabelConnection.Size = new Size(800, 20);
            toolStripStatusLabelConnection.Spring = true;
            toolStripStatusLabelConnection.Text = "Disconnected";
            toolStripStatusLabelConnection.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            btnConnect.Location = new Point(733, 6);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 59);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(427, 37);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(120, 29);
            btnWrite.TabIndex = 10;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            // 
            // txtWriteData
            // 
            txtWriteData.Location = new Point(281, 38);
            txtWriteData.MaxLength = 100;
            txtWriteData.Name = "txtWriteData";
            txtWriteData.PlaceholderText = "Hex (01 or 0102)";
            txtWriteData.Size = new Size(128, 27);
            txtWriteData.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(229, 41);
            label5.Name = "label5";
            label5.Size = new Size(44, 20);
            label5.TabIndex = 8;
            label5.Text = "Data:";
            // 
            // txtWriteAddress
            // 
            txtWriteAddress.Location = new Point(101, 38);
            txtWriteAddress.MaxLength = 6;
            txtWriteAddress.Name = "txtWriteAddress";
            txtWriteAddress.PlaceholderText = "Hex (200100)";
            txtWriteAddress.Size = new Size(110, 27);
            txtWriteAddress.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(18, 41);
            label4.Name = "label4";
            label4.Size = new Size(85, 20);
            label4.TabIndex = 6;
            label4.Text = "Write Addr:";
            // 
            // btnLargeRead
            // 
            btnLargeRead.Location = new Point(323, 4);
            btnLargeRead.Name = "btnLargeRead";
            btnLargeRead.Size = new Size(120, 29);
            btnLargeRead.TabIndex = 3;
            btnLargeRead.Text = "Large Read";
            btnLargeRead.UseVisualStyleBackColor = true;
            btnLargeRead.Click += btnLargeRead_Click;
            // 
            // btnRead
            // 
            btnRead.Location = new Point(197, 4);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(120, 29);
            btnRead.TabIndex = 2;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            // 
            // txtReadAddress
            // 
            txtReadAddress.Location = new Point(103, 5);
            txtReadAddress.MaxLength = 6;
            txtReadAddress.Name = "txtReadAddress";
            txtReadAddress.PlaceholderText = "Hex (e.g., 200100)";
            txtReadAddress.Size = new Size(88, 27);
            txtReadAddress.TabIndex = 1;
            txtReadAddress.Text = "200100";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(18, 8);
            label2.Name = "label2";
            label2.Size = new Size(65, 20);
            label2.TabIndex = 0;
            label2.Text = "Address:";
            // 
            // txtLargeReadAddress
            // 
            txtLargeReadAddress.Location = new Point(0, 0);
            txtLargeReadAddress.Name = "txtLargeReadAddress";
            txtLargeReadAddress.Size = new Size(100, 27);
            txtLargeReadAddress.TabIndex = 0;
            // 
            // label3
            // 
            label3.Location = new Point(0, 0);
            label3.Name = "label3";
            label3.Size = new Size(100, 23);
            label3.TabIndex = 0;
            // 
            // txtOutput
            // 
            txtOutput.BackColor = Color.Black;
            txtOutput.Dock = DockStyle.Fill;
            txtOutput.Font = new Font("Consolas", 9F);
            txtOutput.ForeColor = Color.Lime;
            txtOutput.Location = new Point(0, 97);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(865, 483);
            txtOutput.TabIndex = 0;
            txtOutput.WordWrap = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnConnect);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(btnWrite);
            panel1.Controls.Add(txtReadAddress);
            panel1.Controls.Add(txtWriteData);
            panel1.Controls.Add(btnRead);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(btnLargeRead);
            panel1.Controls.Add(txtWriteAddress);
            panel1.Controls.Add(label4);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 28);
            panel1.Name = "panel1";
            panel1.Size = new Size(865, 69);
            panel1.TabIndex = 5;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(865, 606);
            Controls.Add(txtOutput);
            Controls.Add(panel1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(707, 300);
            Name = "MainForm";
            Text = "Emboidery Serial Communication";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel;
        private Button btnConnect;
        private Label label2;
        private TextBox txtReadAddress;
        private Button btnRead;
        private Button btnLargeRead;
        private TextBox txtLargeReadAddress;
        private Label label3;
        private Button btnWrite;
        private TextBox txtWriteData;
        private Label label5;
        private TextBox txtWriteAddress;
        private Label label4;
        private TextBox txtOutput;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem selectCOMPortToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem connectToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem commandsToolStripMenuItem;
        private ToolStripMenuItem readToolStripMenuItem;
        private ToolStripMenuItem largeReadToolStripMenuItem;
        private ToolStripMenuItem writeToolStripMenuItem;
        private ToolStripMenuItem sumToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLabelConnection;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem switchTo19200BaudToolStripMenuItem;
        private ToolStripMenuItem switchTo57600BaudToolStripMenuItem;
        private ToolStripMenuItem autoSwitchTo57600BaudToolStripMenuItem;
        private ToolStripMenuItem clearOutputToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem memoryViewerToolStripMenuItem;
        private ToolStripMenuItem showSerialTrafficToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private Panel panel1;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem sessionStartToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator6;
    }
}

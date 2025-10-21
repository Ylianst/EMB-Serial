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
            connectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            switchTo19200BaudToolStripMenuItem = new ToolStripMenuItem();
            switchTo57600BaudToolStripMenuItem = new ToolStripMenuItem();
            commandsToolStripMenuItem = new ToolStripMenuItem();
            readToolStripMenuItem = new ToolStripMenuItem();
            largeReadToolStripMenuItem = new ToolStripMenuItem();
            writeToolStripMenuItem = new ToolStripMenuItem();
            loadToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            memoryViewerToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabelConnection = new ToolStripStatusLabel();
            btnDisconnect = new Button();
            btnConnect = new Button();
            comboBoxComPort = new ComboBox();
            label1 = new Label();
            btnLoad = new Button();
            txtLoadLength = new TextBox();
            label7 = new Label();
            txtLoadAddress = new TextBox();
            label6 = new Label();
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
            menuStrip1.Size = new Size(1000, 28);
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
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectToolStripMenuItem, disconnectToolStripMenuItem, toolStripSeparator1, switchTo19200BaudToolStripMenuItem, switchTo57600BaudToolStripMenuItem });
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(98, 24);
            connectionToolStripMenuItem.Text = "&Connection";
            // 
            // connectToolStripMenuItem
            // 
            connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            connectToolStripMenuItem.Size = new Size(235, 26);
            connectToolStripMenuItem.Text = "&Connect";
            connectToolStripMenuItem.Click += connectToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Enabled = false;
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(235, 26);
            disconnectToolStripMenuItem.Text = "&Disconnect";
            disconnectToolStripMenuItem.Click += disconnectToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(232, 6);
            // 
            // switchTo19200BaudToolStripMenuItem
            // 
            switchTo19200BaudToolStripMenuItem.Enabled = false;
            switchTo19200BaudToolStripMenuItem.Name = "switchTo19200BaudToolStripMenuItem";
            switchTo19200BaudToolStripMenuItem.Size = new Size(235, 26);
            switchTo19200BaudToolStripMenuItem.Text = "Switch to &19200 Baud";
            switchTo19200BaudToolStripMenuItem.Click += switchTo19200BaudToolStripMenuItem_Click;
            // 
            // switchTo57600BaudToolStripMenuItem
            // 
            switchTo57600BaudToolStripMenuItem.Enabled = false;
            switchTo57600BaudToolStripMenuItem.Name = "switchTo57600BaudToolStripMenuItem";
            switchTo57600BaudToolStripMenuItem.Size = new Size(235, 26);
            switchTo57600BaudToolStripMenuItem.Text = "Switch to &57600 Baud";
            switchTo57600BaudToolStripMenuItem.Click += switchTo57600BaudToolStripMenuItem_Click;
            // 
            // commandsToolStripMenuItem
            // 
            commandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { readToolStripMenuItem, largeReadToolStripMenuItem, writeToolStripMenuItem, loadToolStripMenuItem, toolStripSeparator3, memoryViewerToolStripMenuItem });
            commandsToolStripMenuItem.Name = "commandsToolStripMenuItem";
            commandsToolStripMenuItem.Size = new Size(98, 24);
            commandsToolStripMenuItem.Text = "C&ommands";
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
            // loadToolStripMenuItem
            // 
            loadToolStripMenuItem.Enabled = false;
            loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            loadToolStripMenuItem.Size = new Size(205, 26);
            loadToolStripMenuItem.Text = "L&oad";
            loadToolStripMenuItem.Click += loadToolStripMenuItem_Click;
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
            statusStrip1.Location = new Point(0, 626);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1000, 26);
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
            toolStripStatusLabelConnection.Size = new Size(935, 20);
            toolStripStatusLabelConnection.Spring = true;
            toolStripStatusLabelConnection.Text = "Disconnected";
            toolStripStatusLabelConnection.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(409, 6);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(120, 29);
            btnDisconnect.TabIndex = 3;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(283, 7);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 29);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // comboBoxComPort
            // 
            comboBoxComPort.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxComPort.FormattingEnabled = true;
            comboBoxComPort.Location = new Point(103, 7);
            comboBoxComPort.Name = "comboBoxComPort";
            comboBoxComPort.Size = new Size(160, 28);
            comboBoxComPort.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(18, 10);
            label1.Name = "label1";
            label1.Size = new Size(75, 20);
            label1.TabIndex = 0;
            label1.Text = "COM Port:";
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(393, 112);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(120, 29);
            btnLoad.TabIndex = 15;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // txtLoadLength
            // 
            txtLoadLength.Location = new Point(288, 113);
            txtLoadLength.MaxLength = 6;
            txtLoadLength.Name = "txtLoadLength";
            txtLoadLength.PlaceholderText = "Hex (000360)";
            txtLoadLength.Size = new Size(85, 27);
            txtLoadLength.TabIndex = 14;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(231, 116);
            label7.Name = "label7";
            label7.Size = new Size(57, 20);
            label7.TabIndex = 13;
            label7.Text = "Length:";
            // 
            // txtLoadAddress
            // 
            txtLoadAddress.Location = new Point(103, 113);
            txtLoadAddress.MaxLength = 6;
            txtLoadAddress.Name = "txtLoadAddress";
            txtLoadAddress.PlaceholderText = "Hex (0240D5)";
            txtLoadAddress.Size = new Size(108, 27);
            txtLoadAddress.TabIndex = 12;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(20, 116);
            label6.Name = "label6";
            label6.Size = new Size(82, 20);
            label6.TabIndex = 11;
            label6.Text = "Load Addr:";
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(427, 76);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(120, 29);
            btnWrite.TabIndex = 10;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            // 
            // txtWriteData
            // 
            txtWriteData.Location = new Point(281, 77);
            txtWriteData.MaxLength = 100;
            txtWriteData.Name = "txtWriteData";
            txtWriteData.PlaceholderText = "Hex (01 or 0102)";
            txtWriteData.Size = new Size(128, 27);
            txtWriteData.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(229, 80);
            label5.Name = "label5";
            label5.Size = new Size(44, 20);
            label5.TabIndex = 8;
            label5.Text = "Data:";
            // 
            // txtWriteAddress
            // 
            txtWriteAddress.Location = new Point(101, 77);
            txtWriteAddress.MaxLength = 6;
            txtWriteAddress.Name = "txtWriteAddress";
            txtWriteAddress.PlaceholderText = "Hex (200100)";
            txtWriteAddress.Size = new Size(110, 27);
            txtWriteAddress.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(20, 80);
            label4.Name = "label4";
            label4.Size = new Size(85, 20);
            label4.TabIndex = 6;
            label4.Text = "Write Addr:";
            // 
            // btnLargeRead
            // 
            btnLargeRead.Location = new Point(323, 41);
            btnLargeRead.Name = "btnLargeRead";
            btnLargeRead.Size = new Size(120, 29);
            btnLargeRead.TabIndex = 3;
            btnLargeRead.Text = "Large Read";
            btnLargeRead.UseVisualStyleBackColor = true;
            btnLargeRead.Click += btnLargeRead_Click;
            // 
            // btnRead
            // 
            btnRead.Location = new Point(197, 41);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(120, 29);
            btnRead.TabIndex = 2;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            // 
            // txtReadAddress
            // 
            txtReadAddress.Location = new Point(103, 42);
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
            label2.Location = new Point(18, 45);
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
            txtOutput.Location = new Point(0, 178);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(1000, 448);
            txtOutput.TabIndex = 0;
            txtOutput.WordWrap = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnLoad);
            panel1.Controls.Add(btnDisconnect);
            panel1.Controls.Add(txtLoadLength);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(btnConnect);
            panel1.Controls.Add(txtLoadAddress);
            panel1.Controls.Add(comboBoxComPort);
            panel1.Controls.Add(label6);
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
            panel1.Size = new Size(1000, 150);
            panel1.TabIndex = 5;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 652);
            Controls.Add(txtOutput);
            Controls.Add(panel1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
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
        private ComboBox comboBoxComPort;
        private Label label1;
        private Button btnConnect;
        private Button btnDisconnect;
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
        private Button btnLoad;
        private TextBox txtLoadAddress;
        private TextBox txtLoadLength;
        private Label label6;
        private Label label7;
        private TextBox txtOutput;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem connectToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem commandsToolStripMenuItem;
        private ToolStripMenuItem readToolStripMenuItem;
        private ToolStripMenuItem largeReadToolStripMenuItem;
        private ToolStripMenuItem writeToolStripMenuItem;
        private ToolStripMenuItem loadToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLabelConnection;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem switchTo19200BaudToolStripMenuItem;
        private ToolStripMenuItem switchTo57600BaudToolStripMenuItem;
        private ToolStripMenuItem clearOutputToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem memoryViewerToolStripMenuItem;
        private ToolStripMenuItem showSerialTrafficToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private Panel panel1;
    }
}

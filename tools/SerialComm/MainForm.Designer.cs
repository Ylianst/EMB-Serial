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
            clearOutputToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            connectToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            switchTo57600BaudToolStripMenuItem = new ToolStripMenuItem();
            commandsToolStripMenuItem = new ToolStripMenuItem();
            readToolStripMenuItem = new ToolStripMenuItem();
            largeReadToolStripMenuItem = new ToolStripMenuItem();
            writeToolStripMenuItem = new ToolStripMenuItem();
            lCommandToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            memoryViewerToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabelConnection = new ToolStripStatusLabel();
            groupBoxConnection = new GroupBox();
            btnDisconnect = new Button();
            btnConnect = new Button();
            comboBoxComPort = new ComboBox();
            label1 = new Label();
            groupBoxCommands = new GroupBox();
            btnLCommand = new Button();
            txtLCommand = new TextBox();
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
            groupBoxOutput = new GroupBox();
            txtOutput = new TextBox();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            groupBoxConnection.SuspendLayout();
            groupBoxCommands.SuspendLayout();
            groupBoxOutput.SuspendLayout();
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
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearOutputToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // clearOutputToolStripMenuItem
            // 
            clearOutputToolStripMenuItem.Name = "clearOutputToolStripMenuItem";
            clearOutputToolStripMenuItem.Size = new Size(180, 26);
            clearOutputToolStripMenuItem.Text = "&Clear Output";
            clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectToolStripMenuItem, disconnectToolStripMenuItem, toolStripSeparator1, switchTo57600BaudToolStripMenuItem });
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
            commandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { readToolStripMenuItem, largeReadToolStripMenuItem, writeToolStripMenuItem, lCommandToolStripMenuItem, toolStripSeparator3, memoryViewerToolStripMenuItem });
            commandsToolStripMenuItem.Name = "commandsToolStripMenuItem";
            commandsToolStripMenuItem.Size = new Size(98, 24);
            commandsToolStripMenuItem.Text = "C&ommands";
            // 
            // readToolStripMenuItem
            // 
            readToolStripMenuItem.Enabled = false;
            readToolStripMenuItem.Name = "readToolStripMenuItem";
            readToolStripMenuItem.Size = new Size(172, 26);
            readToolStripMenuItem.Text = "&Read";
            readToolStripMenuItem.Click += readToolStripMenuItem_Click;
            // 
            // largeReadToolStripMenuItem
            // 
            largeReadToolStripMenuItem.Enabled = false;
            largeReadToolStripMenuItem.Name = "largeReadToolStripMenuItem";
            largeReadToolStripMenuItem.Size = new Size(172, 26);
            largeReadToolStripMenuItem.Text = "&Large Read";
            largeReadToolStripMenuItem.Click += largeReadToolStripMenuItem_Click;
            // 
            // writeToolStripMenuItem
            // 
            writeToolStripMenuItem.Enabled = false;
            writeToolStripMenuItem.Name = "writeToolStripMenuItem";
            writeToolStripMenuItem.Size = new Size(172, 26);
            writeToolStripMenuItem.Text = "&Write";
            writeToolStripMenuItem.Click += writeToolStripMenuItem_Click;
            // 
            // lCommandToolStripMenuItem
            // 
            lCommandToolStripMenuItem.Enabled = false;
            lCommandToolStripMenuItem.Name = "lCommandToolStripMenuItem";
            lCommandToolStripMenuItem.Size = new Size(190, 26);
            lCommandToolStripMenuItem.Text = "L C&ommand";
            lCommandToolStripMenuItem.Click += lCommandToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(187, 6);
            // 
            // memoryViewerToolStripMenuItem
            // 
            memoryViewerToolStripMenuItem.Enabled = false;
            memoryViewerToolStripMenuItem.Name = "memoryViewerToolStripMenuItem";
            memoryViewerToolStripMenuItem.Size = new Size(190, 26);
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
            // groupBoxConnection
            // 
            groupBoxConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxConnection.Controls.Add(btnDisconnect);
            groupBoxConnection.Controls.Add(btnConnect);
            groupBoxConnection.Controls.Add(comboBoxComPort);
            groupBoxConnection.Controls.Add(label1);
            groupBoxConnection.Location = new Point(12, 35);
            groupBoxConnection.Name = "groupBoxConnection";
            groupBoxConnection.Size = new Size(976, 80);
            groupBoxConnection.TabIndex = 2;
            groupBoxConnection.TabStop = false;
            groupBoxConnection.Text = "Connection";
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(409, 32);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(120, 29);
            btnDisconnect.TabIndex = 3;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(283, 32);
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
            comboBoxComPort.Location = new Point(103, 33);
            comboBoxComPort.Name = "comboBoxComPort";
            comboBoxComPort.Size = new Size(160, 28);
            comboBoxComPort.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(18, 36);
            label1.Name = "label1";
            label1.Size = new Size(75, 20);
            label1.TabIndex = 0;
            label1.Text = "COM Port:";
            // 
            // groupBoxCommands
            // 
            groupBoxCommands.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxCommands.Controls.Add(btnLCommand);
            groupBoxCommands.Controls.Add(txtLCommand);
            groupBoxCommands.Controls.Add(label6);
            groupBoxCommands.Controls.Add(btnWrite);
            groupBoxCommands.Controls.Add(txtWriteData);
            groupBoxCommands.Controls.Add(label5);
            groupBoxCommands.Controls.Add(txtWriteAddress);
            groupBoxCommands.Controls.Add(label4);
            groupBoxCommands.Controls.Add(btnLargeRead);
            groupBoxCommands.Controls.Add(btnRead);
            groupBoxCommands.Controls.Add(txtReadAddress);
            groupBoxCommands.Controls.Add(label2);
            groupBoxCommands.Enabled = false;
            groupBoxCommands.Location = new Point(12, 121);
            groupBoxCommands.Name = "groupBoxCommands";
            groupBoxCommands.Size = new Size(976, 143);
            groupBoxCommands.TabIndex = 3;
            groupBoxCommands.TabStop = false;
            groupBoxCommands.Text = "Commands";
            // 
            // btnLCommand
            // 
            btnLCommand.Location = new Point(416, 106);
            btnLCommand.Name = "btnLCommand";
            btnLCommand.Size = new Size(120, 29);
            btnLCommand.TabIndex = 13;
            btnLCommand.Text = "L Command";
            btnLCommand.UseVisualStyleBackColor = true;
            btnLCommand.Click += btnLCommand_Click;
            // 
            // txtLCommand
            // 
            txtLCommand.Location = new Point(128, 107);
            txtLCommand.MaxLength = 12;
            txtLCommand.Name = "txtLCommand";
            txtLCommand.PlaceholderText = "12 hex chars (e.g., 0240D5000360)";
            txtLCommand.Size = new Size(270, 27);
            txtLCommand.TabIndex = 12;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(20, 110);
            label6.Name = "label6";
            label6.Size = new Size(96, 20);
            label6.TabIndex = 11;
            label6.Text = "L Parameters:";
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(427, 70);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(120, 29);
            btnWrite.TabIndex = 10;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            // 
            // txtWriteData
            // 
            txtWriteData.Location = new Point(281, 71);
            txtWriteData.MaxLength = 100;
            txtWriteData.Name = "txtWriteData";
            txtWriteData.PlaceholderText = "Hex data (e.g., 01 or 0102)";
            txtWriteData.Size = new Size(128, 27);
            txtWriteData.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(229, 74);
            label5.Name = "label5";
            label5.Size = new Size(44, 20);
            label5.TabIndex = 8;
            label5.Text = "Data:";
            // 
            // txtWriteAddress
            // 
            txtWriteAddress.Location = new Point(101, 71);
            txtWriteAddress.MaxLength = 6;
            txtWriteAddress.Name = "txtWriteAddress";
            txtWriteAddress.PlaceholderText = "Hex (e.g., 200100)";
            txtWriteAddress.Size = new Size(110, 27);
            txtWriteAddress.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(20, 74);
            label4.Name = "label4";
            label4.Size = new Size(76, 20);
            label4.TabIndex = 6;
            label4.Text = "Write Adr:";
            // 
            // btnLargeRead
            // 
            btnLargeRead.Location = new Point(323, 35);
            btnLargeRead.Name = "btnLargeRead";
            btnLargeRead.Size = new Size(120, 29);
            btnLargeRead.TabIndex = 3;
            btnLargeRead.Text = "Large Read";
            btnLargeRead.UseVisualStyleBackColor = true;
            btnLargeRead.Click += btnLargeRead_Click;
            // 
            // btnRead
            // 
            btnRead.Location = new Point(197, 35);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(120, 29);
            btnRead.TabIndex = 2;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            // 
            // txtReadAddress
            // 
            txtReadAddress.Location = new Point(103, 36);
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
            label2.Location = new Point(18, 39);
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
            // groupBoxOutput
            // 
            groupBoxOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxOutput.Controls.Add(txtOutput);
            groupBoxOutput.Location = new Point(12, 279);
            groupBoxOutput.Name = "groupBoxOutput";
            groupBoxOutput.Size = new Size(976, 344);
            groupBoxOutput.TabIndex = 4;
            groupBoxOutput.TabStop = false;
            groupBoxOutput.Text = "Output";
            // 
            // txtOutput
            // 
            txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtOutput.BackColor = Color.Black;
            txtOutput.Font = new Font("Consolas", 9F);
            txtOutput.ForeColor = Color.Lime;
            txtOutput.Location = new Point(6, 26);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Both;
            txtOutput.Size = new Size(964, 312);
            txtOutput.TabIndex = 0;
            txtOutput.WordWrap = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 652);
            Controls.Add(groupBoxOutput);
            Controls.Add(groupBoxCommands);
            Controls.Add(groupBoxConnection);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "Bernina Serial Communication";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBoxConnection.ResumeLayout(false);
            groupBoxConnection.PerformLayout();
            groupBoxCommands.ResumeLayout(false);
            groupBoxCommands.PerformLayout();
            groupBoxOutput.ResumeLayout(false);
            groupBoxOutput.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel;
        private GroupBox groupBoxConnection;
        private ComboBox comboBoxComPort;
        private Label label1;
        private Button btnConnect;
        private Button btnDisconnect;
        private GroupBox groupBoxCommands;
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
        private Button btnLCommand;
        private TextBox txtLCommand;
        private Label label6;
        private GroupBox groupBoxOutput;
        private TextBox txtOutput;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem connectToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem commandsToolStripMenuItem;
        private ToolStripMenuItem readToolStripMenuItem;
        private ToolStripMenuItem largeReadToolStripMenuItem;
        private ToolStripMenuItem writeToolStripMenuItem;
        private ToolStripMenuItem lCommandToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLabelConnection;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem switchTo57600BaudToolStripMenuItem;
        private ToolStripMenuItem clearOutputToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem memoryViewerToolStripMenuItem;
    }
}

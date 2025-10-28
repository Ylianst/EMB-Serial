namespace SerialComm
{
    partial class DebugForm
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
            exitToolStripMenuItem = new ToolStripMenuItem();
            commandsToolStripMenuItem = new ToolStripMenuItem();
            readToolStripMenuItem = new ToolStripMenuItem();
            largeReadToolStripMenuItem = new ToolStripMenuItem();
            writeToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            sumToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            sessionStartToolStripMenuItem = new ToolStripMenuItem();
            sessionEndToolStripMenuItem = new ToolStripMenuItem();
            protocolResetToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            firmwareInfoToolStripMenuItem = new ToolStripMenuItem();
            readEmbroideryFilesToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            clearOutputToolStripMenuItem = new ToolStripMenuItem();
            showSerialTrafficToolStripMenuItem = new ToolStripMenuItem();
            showDebugToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            controlPanel = new Panel();
            lblReadAddress = new Label();
            txtReadAddress = new TextBox();
            btnRead = new Button();
            btnLargeRead = new Button();
            lblWriteAddress = new Label();
            txtWriteAddress = new TextBox();
            lblWriteData = new Label();
            txtWriteData = new TextBox();
            btnWrite = new Button();
            txtOutput = new TextBox();
            clearButton = new Button();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            controlPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, commandsToolStripMenuItem, viewToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 3, 0, 3);
            menuStrip.Size = new Size(733, 30);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(224, 26);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // commandsToolStripMenuItem
            // 
            commandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { readToolStripMenuItem, largeReadToolStripMenuItem, writeToolStripMenuItem, toolStripSeparator1, sumToolStripMenuItem, toolStripSeparator2, sessionStartToolStripMenuItem, sessionEndToolStripMenuItem, protocolResetToolStripMenuItem, toolStripSeparator3, firmwareInfoToolStripMenuItem, readEmbroideryFilesToolStripMenuItem });
            commandsToolStripMenuItem.Name = "commandsToolStripMenuItem";
            commandsToolStripMenuItem.Size = new Size(98, 24);
            commandsToolStripMenuItem.Text = "&Commands";
            // 
            // readToolStripMenuItem
            // 
            readToolStripMenuItem.Name = "readToolStripMenuItem";
            readToolStripMenuItem.Size = new Size(240, 26);
            readToolStripMenuItem.Text = "&Read";
            readToolStripMenuItem.Click += readToolStripMenuItem_Click;
            // 
            // largeReadToolStripMenuItem
            // 
            largeReadToolStripMenuItem.Name = "largeReadToolStripMenuItem";
            largeReadToolStripMenuItem.Size = new Size(240, 26);
            largeReadToolStripMenuItem.Text = "L&arge Read";
            largeReadToolStripMenuItem.Click += largeReadToolStripMenuItem_Click;
            // 
            // writeToolStripMenuItem
            // 
            writeToolStripMenuItem.Name = "writeToolStripMenuItem";
            writeToolStripMenuItem.Size = new Size(240, 26);
            writeToolStripMenuItem.Text = "&Write";
            writeToolStripMenuItem.Click += writeToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(237, 6);
            // 
            // sumToolStripMenuItem
            // 
            sumToolStripMenuItem.Name = "sumToolStripMenuItem";
            sumToolStripMenuItem.Size = new Size(240, 26);
            sumToolStripMenuItem.Text = "&Sum";
            sumToolStripMenuItem.Click += sumToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(237, 6);
            // 
            // sessionStartToolStripMenuItem
            // 
            sessionStartToolStripMenuItem.Name = "sessionStartToolStripMenuItem";
            sessionStartToolStripMenuItem.Size = new Size(240, 26);
            sessionStartToolStripMenuItem.Text = "Session &Start";
            sessionStartToolStripMenuItem.Click += sessionStartToolStripMenuItem_Click;
            // 
            // sessionEndToolStripMenuItem
            // 
            sessionEndToolStripMenuItem.Name = "sessionEndToolStripMenuItem";
            sessionEndToolStripMenuItem.Size = new Size(240, 26);
            sessionEndToolStripMenuItem.Text = "Session &End";
            sessionEndToolStripMenuItem.Click += sessionEndToolStripMenuItem_Click;
            // 
            // protocolResetToolStripMenuItem
            // 
            protocolResetToolStripMenuItem.Name = "protocolResetToolStripMenuItem";
            protocolResetToolStripMenuItem.Size = new Size(240, 26);
            protocolResetToolStripMenuItem.Text = "Protocol &Reset";
            protocolResetToolStripMenuItem.Click += protocolResetToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(237, 6);
            // 
            // firmwareInfoToolStripMenuItem
            // 
            firmwareInfoToolStripMenuItem.Name = "firmwareInfoToolStripMenuItem";
            firmwareInfoToolStripMenuItem.Size = new Size(240, 26);
            firmwareInfoToolStripMenuItem.Text = "&Firmware Info";
            firmwareInfoToolStripMenuItem.Click += firmwareInfoToolStripMenuItem_Click;
            // 
            // readEmbroideryFilesToolStripMenuItem
            // 
            readEmbroideryFilesToolStripMenuItem.Name = "readEmbroideryFilesToolStripMenuItem";
            readEmbroideryFilesToolStripMenuItem.Size = new Size(240, 26);
            readEmbroideryFilesToolStripMenuItem.Text = "Read &Embroidery Files";
            readEmbroideryFilesToolStripMenuItem.Click += readEmbroideryFilesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearOutputToolStripMenuItem, showSerialTrafficToolStripMenuItem, showDebugToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(55, 24);
            viewToolStripMenuItem.Text = "&View";
            // 
            // clearOutputToolStripMenuItem
            // 
            clearOutputToolStripMenuItem.Name = "clearOutputToolStripMenuItem";
            clearOutputToolStripMenuItem.Size = new Size(245, 26);
            clearOutputToolStripMenuItem.Text = "&Clear Output";
            clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
            // 
            // showSerialTrafficToolStripMenuItem
            // 
            showSerialTrafficToolStripMenuItem.CheckOnClick = true;
            showSerialTrafficToolStripMenuItem.Name = "showSerialTrafficToolStripMenuItem";
            showSerialTrafficToolStripMenuItem.Size = new Size(245, 26);
            showSerialTrafficToolStripMenuItem.Text = "Show &Serial Traffic";
            showSerialTrafficToolStripMenuItem.Click += showSerialTrafficToolStripMenuItem_Click;
            // 
            // showDebugToolStripMenuItem
            // 
            showDebugToolStripMenuItem.CheckOnClick = true;
            showDebugToolStripMenuItem.Name = "showDebugToolStripMenuItem";
            showDebugToolStripMenuItem.Size = new Size(245, 26);
            showDebugToolStripMenuItem.Text = "Show De&bug Messages";
            showDebugToolStripMenuItem.Click += showDebugToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip.Location = new Point(0, 484);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(733, 26);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(158, 20);
            toolStripStatusLabel.Text = "Debug Window Ready";
            // 
            // controlPanel
            // 
            controlPanel.AutoScroll = true;
            controlPanel.Controls.Add(clearButton);
            controlPanel.Controls.Add(lblReadAddress);
            controlPanel.Controls.Add(txtReadAddress);
            controlPanel.Controls.Add(btnRead);
            controlPanel.Controls.Add(btnLargeRead);
            controlPanel.Controls.Add(lblWriteAddress);
            controlPanel.Controls.Add(txtWriteAddress);
            controlPanel.Controls.Add(lblWriteData);
            controlPanel.Controls.Add(txtWriteData);
            controlPanel.Controls.Add(btnWrite);
            controlPanel.Dock = DockStyle.Top;
            controlPanel.Location = new Point(0, 30);
            controlPanel.Margin = new Padding(3, 4, 3, 4);
            controlPanel.Name = "controlPanel";
            controlPanel.Padding = new Padding(11, 13, 11, 13);
            controlPanel.Size = new Size(733, 79);
            controlPanel.TabIndex = 0;
            // 
            // lblReadAddress
            // 
            lblReadAddress.AutoSize = true;
            lblReadAddress.Location = new Point(11, 13);
            lblReadAddress.Name = "lblReadAddress";
            lblReadAddress.Size = new Size(143, 20);
            lblReadAddress.TabIndex = 0;
            lblReadAddress.Text = "Read Address (Hex):";
            // 
            // txtReadAddress
            // 
            txtReadAddress.Location = new Point(161, 10);
            txtReadAddress.Margin = new Padding(3, 4, 3, 4);
            txtReadAddress.Name = "txtReadAddress";
            txtReadAddress.Size = new Size(171, 27);
            txtReadAddress.TabIndex = 1;
            txtReadAddress.Text = "200100";
            // 
            // btnRead
            // 
            btnRead.Location = new Point(338, 10);
            btnRead.Margin = new Padding(3, 4, 3, 4);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(86, 31);
            btnRead.TabIndex = 2;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            // 
            // btnLargeRead
            // 
            btnLargeRead.Location = new Point(430, 10);
            btnLargeRead.Margin = new Padding(3, 4, 3, 4);
            btnLargeRead.Name = "btnLargeRead";
            btnLargeRead.Size = new Size(103, 31);
            btnLargeRead.TabIndex = 3;
            btnLargeRead.Text = "Large Read";
            btnLargeRead.UseVisualStyleBackColor = true;
            btnLargeRead.Click += btnLargeRead_Click;
            // 
            // lblWriteAddress
            // 
            lblWriteAddress.AutoSize = true;
            lblWriteAddress.Location = new Point(11, 45);
            lblWriteAddress.Name = "lblWriteAddress";
            lblWriteAddress.Size = new Size(145, 20);
            lblWriteAddress.TabIndex = 4;
            lblWriteAddress.Text = "Write Address (Hex):";
            // 
            // txtWriteAddress
            // 
            txtWriteAddress.Location = new Point(161, 42);
            txtWriteAddress.Margin = new Padding(3, 4, 3, 4);
            txtWriteAddress.Name = "txtWriteAddress";
            txtWriteAddress.Size = new Size(171, 27);
            txtWriteAddress.TabIndex = 5;
            // 
            // lblWriteData
            // 
            lblWriteData.AutoSize = true;
            lblWriteData.Location = new Point(340, 45);
            lblWriteData.Name = "lblWriteData";
            lblWriteData.Size = new Size(84, 20);
            lblWriteData.TabIndex = 6;
            lblWriteData.Text = "Data (Hex):";
            // 
            // txtWriteData
            // 
            txtWriteData.Location = new Point(430, 42);
            txtWriteData.Margin = new Padding(3, 4, 3, 4);
            txtWriteData.Name = "txtWriteData";
            txtWriteData.Size = new Size(199, 27);
            txtWriteData.TabIndex = 7;
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(635, 40);
            btnWrite.Margin = new Padding(3, 4, 3, 4);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(86, 31);
            btnWrite.TabIndex = 8;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            // 
            // txtOutput
            // 
            txtOutput.BackColor = Color.Black;
            txtOutput.Dock = DockStyle.Fill;
            txtOutput.Font = new Font("Courier New", 9F);
            txtOutput.ForeColor = Color.Lime;
            txtOutput.Location = new Point(0, 109);
            txtOutput.Margin = new Padding(3, 4, 3, 4);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Both;
            txtOutput.Size = new Size(733, 375);
            txtOutput.TabIndex = 0;
            txtOutput.WordWrap = false;
            // 
            // clearButton
            // 
            clearButton.Location = new Point(635, 6);
            clearButton.Margin = new Padding(3, 4, 3, 4);
            clearButton.Name = "clearButton";
            clearButton.Size = new Size(86, 31);
            clearButton.TabIndex = 9;
            clearButton.Text = "Clear";
            clearButton.UseVisualStyleBackColor = true;
            clearButton.Click += clearOutputToolStripMenuItem_Click;
            // 
            // DebugForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(733, 510);
            Controls.Add(txtOutput);
            Controls.Add(controlPanel);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(751, 445);
            Name = "DebugForm";
            Text = "Developer Debug";
            FormClosing += DebugForm_FormClosing;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            controlPanel.ResumeLayout(false);
            controlPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem commandsToolStripMenuItem;
        private ToolStripMenuItem readToolStripMenuItem;
        private ToolStripMenuItem largeReadToolStripMenuItem;
        private ToolStripMenuItem writeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem sumToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem sessionStartToolStripMenuItem;
        private ToolStripMenuItem sessionEndToolStripMenuItem;
        private ToolStripMenuItem protocolResetToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem firmwareInfoToolStripMenuItem;
        private ToolStripMenuItem readEmbroideryFilesToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem clearOutputToolStripMenuItem;
        private ToolStripMenuItem showSerialTrafficToolStripMenuItem;
        private ToolStripMenuItem showDebugToolStripMenuItem;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusLabel;
        private Panel controlPanel;
        private Label lblReadAddress;
        private TextBox txtReadAddress;
        private Button btnRead;
        private Button btnLargeRead;
        private Label lblWriteAddress;
        private TextBox txtWriteAddress;
        private Label lblWriteData;
        private TextBox txtWriteData;
        private Button btnWrite;
        private TextBox txtOutput;
        private Button clearButton;
    }
}

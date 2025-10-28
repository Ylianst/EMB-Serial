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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DebugForm));
            
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
            
            splitContainer = new SplitContainer();
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
            
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            controlPanel.SuspendLayout();
            SuspendLayout();
            
            // menuStrip
            menuStrip.Items.AddRange(new ToolStripItem[] {
                fileToolStripMenuItem,
                commandsToolStripMenuItem,
                viewToolStripMenuItem
            });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(800, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip1";
            
            // fileToolStripMenuItem
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                exitToolStripMenuItem
            });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            
            // exitToolStripMenuItem
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(92, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            
            // commandsToolStripMenuItem
            commandsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                readToolStripMenuItem,
                largeReadToolStripMenuItem,
                writeToolStripMenuItem,
                toolStripSeparator1,
                sumToolStripMenuItem,
                toolStripSeparator2,
                sessionStartToolStripMenuItem,
                sessionEndToolStripMenuItem,
                protocolResetToolStripMenuItem,
                toolStripSeparator3,
                firmwareInfoToolStripMenuItem,
                readEmbroideryFilesToolStripMenuItem
            });
            commandsToolStripMenuItem.Name = "commandsToolStripMenuItem";
            commandsToolStripMenuItem.Size = new Size(81, 20);
            commandsToolStripMenuItem.Text = "&Commands";
            
            // readToolStripMenuItem
            readToolStripMenuItem.Name = "readToolStripMenuItem";
            readToolStripMenuItem.Size = new Size(180, 22);
            readToolStripMenuItem.Text = "&Read";
            readToolStripMenuItem.Click += readToolStripMenuItem_Click;
            
            // largeReadToolStripMenuItem
            largeReadToolStripMenuItem.Name = "largeReadToolStripMenuItem";
            largeReadToolStripMenuItem.Size = new Size(180, 22);
            largeReadToolStripMenuItem.Text = "L&arge Read";
            largeReadToolStripMenuItem.Click += largeReadToolStripMenuItem_Click;
            
            // writeToolStripMenuItem
            writeToolStripMenuItem.Name = "writeToolStripMenuItem";
            writeToolStripMenuItem.Size = new Size(180, 22);
            writeToolStripMenuItem.Text = "&Write";
            writeToolStripMenuItem.Click += writeToolStripMenuItem_Click;
            
            // toolStripSeparator1
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            
            // sumToolStripMenuItem
            sumToolStripMenuItem.Name = "sumToolStripMenuItem";
            sumToolStripMenuItem.Size = new Size(180, 22);
            sumToolStripMenuItem.Text = "&Sum";
            sumToolStripMenuItem.Click += sumToolStripMenuItem_Click;
            
            // toolStripSeparator2
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(177, 6);
            
            // sessionStartToolStripMenuItem
            sessionStartToolStripMenuItem.Name = "sessionStartToolStripMenuItem";
            sessionStartToolStripMenuItem.Size = new Size(180, 22);
            sessionStartToolStripMenuItem.Text = "Session &Start";
            sessionStartToolStripMenuItem.Click += sessionStartToolStripMenuItem_Click;
            
            // sessionEndToolStripMenuItem
            sessionEndToolStripMenuItem.Name = "sessionEndToolStripMenuItem";
            sessionEndToolStripMenuItem.Size = new Size(180, 22);
            sessionEndToolStripMenuItem.Text = "Session &End";
            sessionEndToolStripMenuItem.Click += sessionEndToolStripMenuItem_Click;
            
            // protocolResetToolStripMenuItem
            protocolResetToolStripMenuItem.Name = "protocolResetToolStripMenuItem";
            protocolResetToolStripMenuItem.Size = new Size(180, 22);
            protocolResetToolStripMenuItem.Text = "Protocol &Reset";
            protocolResetToolStripMenuItem.Click += protocolResetToolStripMenuItem_Click;
            
            // toolStripSeparator3
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(177, 6);
            
            // firmwareInfoToolStripMenuItem
            firmwareInfoToolStripMenuItem.Name = "firmwareInfoToolStripMenuItem";
            firmwareInfoToolStripMenuItem.Size = new Size(180, 22);
            firmwareInfoToolStripMenuItem.Text = "&Firmware Info";
            firmwareInfoToolStripMenuItem.Click += firmwareInfoToolStripMenuItem_Click;
            
            // readEmbroideryFilesToolStripMenuItem
            readEmbroideryFilesToolStripMenuItem.Name = "readEmbroideryFilesToolStripMenuItem";
            readEmbroideryFilesToolStripMenuItem.Size = new Size(180, 22);
            readEmbroideryFilesToolStripMenuItem.Text = "Read &Embroidery Files";
            readEmbroideryFilesToolStripMenuItem.Click += readEmbroideryFilesToolStripMenuItem_Click;
            
            // viewToolStripMenuItem
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                clearOutputToolStripMenuItem,
                showSerialTrafficToolStripMenuItem,
                showDebugToolStripMenuItem
            });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            
            // clearOutputToolStripMenuItem
            clearOutputToolStripMenuItem.Name = "clearOutputToolStripMenuItem";
            clearOutputToolStripMenuItem.Size = new Size(180, 22);
            clearOutputToolStripMenuItem.Text = "&Clear Output";
            clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
            
            // showSerialTrafficToolStripMenuItem
            showSerialTrafficToolStripMenuItem.Checked = false;
            showSerialTrafficToolStripMenuItem.CheckOnClick = true;
            showSerialTrafficToolStripMenuItem.Name = "showSerialTrafficToolStripMenuItem";
            showSerialTrafficToolStripMenuItem.Size = new Size(180, 22);
            showSerialTrafficToolStripMenuItem.Text = "Show &Serial Traffic";
            showSerialTrafficToolStripMenuItem.Click += showSerialTrafficToolStripMenuItem_Click;
            
            // showDebugToolStripMenuItem
            showDebugToolStripMenuItem.Checked = false;
            showDebugToolStripMenuItem.CheckOnClick = true;
            showDebugToolStripMenuItem.Name = "showDebugToolStripMenuItem";
            showDebugToolStripMenuItem.Size = new Size(180, 22);
            showDebugToolStripMenuItem.Text = "Show De&bug Messages";
            showDebugToolStripMenuItem.Click += showDebugToolStripMenuItem_Click;
            
            // statusStrip
            statusStrip.Items.AddRange(new ToolStripItem[] {
                toolStripStatusLabel
            });
            statusStrip.Location = new Point(0, 539);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(800, 22);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            
            // toolStripStatusLabel
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(118, 17);
            toolStripStatusLabel.Text = "Debug Window Ready";
            
            // splitContainer
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 24);
            splitContainer.Name = "splitContainer";
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.Panel1.Controls.Add(controlPanel);
            splitContainer.Panel1MinSize = 100;
            splitContainer.Panel2.Controls.Add(txtOutput);
            splitContainer.Panel2MinSize = 100;
            splitContainer.Size = new Size(800, 515);
            splitContainer.SplitterDistance = 150;
            splitContainer.TabIndex = 1;
            
            // controlPanel
            controlPanel.AutoScroll = true;
            controlPanel.Controls.AddRange(new Control[] {
                lblReadAddress,
                txtReadAddress,
                btnRead,
                btnLargeRead,
                lblWriteAddress,
                txtWriteAddress,
                lblWriteData,
                txtWriteData,
                btnWrite
            });
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.Location = new Point(0, 0);
            controlPanel.Name = "controlPanel";
            controlPanel.Padding = new Padding(10);
            controlPanel.Size = new Size(800, 150);
            controlPanel.TabIndex = 0;
            
            // lblReadAddress
            lblReadAddress.AutoSize = true;
            lblReadAddress.Location = new Point(10, 10);
            lblReadAddress.Name = "lblReadAddress";
            lblReadAddress.Size = new Size(95, 15);
            lblReadAddress.TabIndex = 0;
            lblReadAddress.Text = "Read Address (Hex):";
            
            // txtReadAddress
            txtReadAddress.Location = new Point(10, 28);
            txtReadAddress.Name = "txtReadAddress";
            txtReadAddress.Size = new Size(150, 23);
            txtReadAddress.TabIndex = 1;
            txtReadAddress.Text = "200100";
            
            // btnRead
            btnRead.Location = new Point(170, 28);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(75, 23);
            btnRead.TabIndex = 2;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            
            // btnLargeRead
            btnLargeRead.Location = new Point(255, 28);
            btnLargeRead.Name = "btnLargeRead";
            btnLargeRead.Size = new Size(90, 23);
            btnLargeRead.TabIndex = 3;
            btnLargeRead.Text = "Large Read";
            btnLargeRead.UseVisualStyleBackColor = true;
            btnLargeRead.Click += btnLargeRead_Click;
            
            // lblWriteAddress
            lblWriteAddress.AutoSize = true;
            lblWriteAddress.Location = new Point(10, 65);
            lblWriteAddress.Name = "lblWriteAddress";
            lblWriteAddress.Size = new Size(98, 15);
            lblWriteAddress.TabIndex = 4;
            lblWriteAddress.Text = "Write Address (Hex):";
            
            // txtWriteAddress
            txtWriteAddress.Location = new Point(10, 83);
            txtWriteAddress.Name = "txtWriteAddress";
            txtWriteAddress.Size = new Size(150, 23);
            txtWriteAddress.TabIndex = 5;
            
            // lblWriteData
            lblWriteData.AutoSize = true;
            lblWriteData.Location = new Point(170, 65);
            lblWriteData.Name = "lblWriteData";
            lblWriteData.Size = new Size(75, 15);
            lblWriteData.TabIndex = 6;
            lblWriteData.Text = "Data (Hex):";
            
            // txtWriteData
            txtWriteData.Location = new Point(170, 83);
            txtWriteData.Name = "txtWriteData";
            txtWriteData.Size = new Size(175, 23);
            txtWriteData.TabIndex = 7;
            
            // btnWrite
            btnWrite.Location = new Point(355, 83);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(75, 23);
            btnWrite.TabIndex = 8;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            
            // txtOutput
            txtOutput.BackColor = Color.Black;
            txtOutput.ForeColor = Color.Lime;
            txtOutput.Dock = DockStyle.Fill;
            txtOutput.Font = new Font("Courier New", 9F);
            txtOutput.Location = new Point(0, 0);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.ScrollBars = ScrollBars.Both;
            txtOutput.Size = new Size(800, 365);
            txtOutput.TabIndex = 0;
            txtOutput.WordWrap = false;
            
            // DebugForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 561);
            Controls.Add(splitContainer);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
            Name = "DebugForm";
            Text = "Developer Debug";
            FormClosing += DebugForm_FormClosing;
            
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
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
        private SplitContainer splitContainer;
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
    }
}

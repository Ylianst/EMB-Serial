namespace EmbroideryCommunicator
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            lblAppName = new Label();
            lblVersion = new Label();
            btnOk = new Button();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            linkLabel1 = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAppName.Location = new Point(20, 20);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new Size(267, 28);
            lblAppName.TabIndex = 0;
            lblAppName.Text = "Embroidery Communicator";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(20, 60);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(102, 20);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version 1.0.0.0";
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(426, 168);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(94, 29);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(368, 20);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(152, 137);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 84);
            label1.Name = "label1";
            label1.Size = new Size(226, 20);
            label1.TabIndex = 4;
            label1.Text = "Open Source, Apache 2.0 License";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(20, 108);
            label2.Name = "label2";
            label2.Size = new Size(128, 20);
            label2.TabIndex = 5;
            label2.Text = "Ylian Saint-Hilaire";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(20, 137);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(212, 20);
            linkLabel1.TabIndex = 6;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "github.com/Ylianst/EMB-Serial";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // AboutForm
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(532, 209);
            Controls.Add(linkLabel1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Controls.Add(btnOk);
            Controls.Add(lblVersion);
            Controls.Add(lblAppName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblAppName;
        private Label lblVersion;
        private Button btnOk;
        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
        private LinkLabel linkLabel1;
    }
}

namespace SerialComm
{
    partial class SumDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblAddress = new Label();
            txtAddress = new TextBox();
            lblLength = new Label();
            txtLength = new TextBox();
            btnOK = new Button();
            btnCancel = new Button();
            lblInfo = new Label();
            SuspendLayout();
            // 
            // lblAddress
            // 
            lblAddress.AutoSize = true;
            lblAddress.Location = new Point(12, 50);
            lblAddress.Name = "lblAddress";
            lblAddress.Size = new Size(95, 20);
            lblAddress.TabIndex = 0;
            lblAddress.Text = "Address (hex):";
            // 
            // txtAddress
            // 
            txtAddress.Location = new Point(120, 47);
            txtAddress.MaxLength = 6;
            txtAddress.Name = "txtAddress";
            txtAddress.Size = new Size(150, 27);
            txtAddress.TabIndex = 1;
            // 
            // lblLength
            // 
            lblLength.AutoSize = true;
            lblLength.Location = new Point(12, 90);
            lblLength.Name = "lblLength";
            lblLength.Size = new Size(88, 20);
            lblLength.TabIndex = 2;
            lblLength.Text = "Length (hex):";
            // 
            // txtLength
            // 
            txtLength.Location = new Point(120, 87);
            txtLength.MaxLength = 6;
            txtLength.Name = "txtLength";
            txtLength.Size = new Size(150, 27);
            txtLength.TabIndex = 3;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(60, 140);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 30);
            btnOK.TabIndex = 4;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(160, 140);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblInfo
            // 
            lblInfo.Location = new Point(12, 9);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(296, 30);
            lblInfo.TabIndex = 6;
            lblInfo.Text = "Enter address and length in hexadecimal format:";
            // 
            // SumDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(320, 185);
            Controls.Add(lblInfo);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(txtLength);
            Controls.Add(lblLength);
            Controls.Add(txtAddress);
            Controls.Add(lblAddress);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SumDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Sum Command";
            Load += SumDialog_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblAddress;
        private TextBox txtAddress;
        private Label lblLength;
        private TextBox txtLength;
        private Button btnOK;
        private Button btnCancel;
        private Label lblInfo;
    }
}

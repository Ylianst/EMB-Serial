using System;
using System.Windows.Forms;

namespace EmbroideryCommunicator
{
    public partial class SumDialog : Form
    {
        public int Address { get; private set; }
        public int Length { get; private set; }

        public SumDialog()
        {
            InitializeComponent();
        }

        private void SumDialog_Load(object sender, EventArgs e)
        {
            // Set default values
            txtAddress.Text = "0240D5";
            txtLength.Text = "000360";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string addressStr = txtAddress.Text.Trim();
            string lengthStr = txtLength.Text.Trim();

            // Validate address
            if (string.IsNullOrWhiteSpace(addressStr))
            {
                MessageBox.Show("Please enter an address.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAddress.Focus();
                return;
            }

            if (!int.TryParse(addressStr, System.Globalization.NumberStyles.HexNumber, null, out int address))
            {
                MessageBox.Show("Invalid address format. Use hex format (e.g., 0240D5).", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAddress.Focus();
                return;
            }

            // Validate length
            if (string.IsNullOrWhiteSpace(lengthStr))
            {
                MessageBox.Show("Please enter a length.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLength.Focus();
                return;
            }

            if (!int.TryParse(lengthStr, System.Globalization.NumberStyles.HexNumber, null, out int length) || length <= 0)
            {
                MessageBox.Show("Invalid length format. Use hex format (e.g., 000360) and must be greater than 0.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLength.Focus();
                return;
            }

            Address = address;
            Length = length;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

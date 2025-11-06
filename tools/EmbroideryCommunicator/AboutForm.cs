using System;
using System.Reflection;
using System.Windows.Forms;

namespace EmbroideryCommunicator
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            LoadApplicationInfo();
        }

        private void LoadApplicationInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get version
            Version? version = assembly.GetName().Version;
            string versionString = version != null ? version.ToString() : "1.0.0.0";

            lblVersion.Text = $"Version {versionString}";
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Ylianst/EMB-Serial",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

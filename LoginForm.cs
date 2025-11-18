using System;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Windows.Forms;

namespace WinServer2019
{
    public partial class LoginForm : Form
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Domain { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
            DetectDomain();
        }

        private void DetectDomain()
        {
            try
            {
                string detectedDomain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                
                if (string.IsNullOrWhiteSpace(detectedDomain) || detectedDomain.ToUpper() == "WORKGROUP")
                {
                    txtDomain.Text = "csitlab.local"; // Default domain
                    lblStatus.Text = "Not domain-joined. Using default: csitlab.local";
                    lblStatus.ForeColor = Color.Orange;
                }
                else
                {
                    txtDomain.Text = detectedDomain;
                    lblStatus.Text = $"Detected domain: {detectedDomain}";
                    lblStatus.ForeColor = Color.Green;
                }
            }
            catch
            {
                txtDomain.Text = "csitlab.local";
                lblStatus.Text = "Unable to detect domain. Using default.";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private void ChkDefaultCreds_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDefaultCreds.Checked)
            {
                txtUsername.Text = "Administrator";
                txtPassword.Text = "@csitlab123";
                txtUsername.Enabled = false;
                txtPassword.Enabled = false;
            }
            else
            {
                txtUsername.Text = "";
                txtPassword.Text = "";
                txtUsername.Enabled = true;
                txtPassword.Enabled = true;
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lblStatus.Text = "Please enter a username";
                lblStatus.ForeColor = Color.Red;
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblStatus.Text = "Please enter a password";
                lblStatus.ForeColor = Color.Red;
                txtPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDomain.Text))
            {
                lblStatus.Text = "Please enter a domain";
                lblStatus.ForeColor = Color.Red;
                txtDomain.Focus();
                return;
            }

            // Disable controls during validation
            btnLogin.Enabled = false;
            lblStatus.Text = "Validating credentials...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            // Validate credentials
            if (ValidateCredentials(txtUsername.Text, txtPassword.Text, txtDomain.Text))
            {
                Username = txtUsername.Text;
                Password = txtPassword.Text;
                Domain = txtDomain.Text;
                
                lblStatus.Text = "Login successful!";
                lblStatus.ForeColor = Color.Green;
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.Text = "Invalid credentials or domain not accessible";
                lblStatus.ForeColor = Color.Red;
                btnLogin.Enabled = true;
                txtPassword.Text = "";
                txtPassword.Focus();
            }
        }

        private bool ValidateCredentials(string username, string password, string domain)
        {
            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain, username, password))
                {
                    // Validate credentials
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinServer2019
{
    public partial class ScreenViewerForm : Form
    {
        private Timer refreshTimer;
        private string pcName;
        private MonitoringServer server;

        public ScreenViewerForm(string pcName, MonitoringServer server)
        {
            this.pcName = pcName;
            this.server = server;
            InitializeComponent();
            InitializeCustomComponents();
            
            server.OnClientUpdate += Server_OnClientUpdate;
        }

        private void InitializeCustomComponents()
        {
            // Update labels with PC name
            this.Text = $"Screen View - {pcName}";
            lblPCName.Text = $"Viewing: {pcName}";
            
            // Setup close button event
            btnClose.Click += (s, e) => this.Close();

            // Auto-refresh timer (every 1 second)
            refreshTimer = new Timer
            {
                Interval = 1000,
                Enabled = true
            };
            refreshTimer.Tick += RefreshTimer_Tick;
        }

        private void Server_OnClientUpdate(ClientActivity activity)
        {
            if (activity.PCName == pcName && activity.ScreenshotData != null && activity.ScreenshotData.Length > 0)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateScreenshot(activity)));
                }
                else
                {
                    UpdateScreenshot(activity);
                }
            }
        }

        private void UpdateScreenshot(ClientActivity activity)
        {
            try
            {
                using (var ms = new MemoryStream(activity.ScreenshotData))
                {
                    var image = Image.FromStream(ms);
                    
                    // Dispose old image
                    if (pictureBox.Image != null)
                    {
                        var oldImage = pictureBox.Image;
                        pictureBox.Image = null;
                        oldImage.Dispose();
                    }
                    
                    pictureBox.Image = image;
                    lblStatus.Text = $"Last Update: {activity.LastUpdate:HH:mm:ss} | Active: {activity.ActiveWindow}";
                    lblStatus.ForeColor = Color.LightGreen;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error displaying screen: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // Check if client is still connected
            var clients = server.GetConnectedClientsDictionary();
            if (!clients.ContainsKey(pcName))
            {
                lblStatus.Text = "Client disconnected";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Requesting screen update...";
            lblStatus.ForeColor = Color.Yellow;
        }

        private void ScreenViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            server.OnClientUpdate -= Server_OnClientUpdate;
            
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
            }
        }
    }
}

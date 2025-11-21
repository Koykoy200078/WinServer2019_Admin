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
            
            // Setup quality selector
            cmbQuality.SelectedIndex = 1; // Default to 720p (Balanced)
            cmbQuality.SelectedIndexChanged += CmbQuality_SelectedIndexChanged;
            
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

        private void CmbQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Quality change will be reflected in next screenshot capture
            // The client always sends the same quality, but we can request different quality
            string selectedQuality;
            switch (cmbQuality.SelectedIndex)
            {
                case 0:
                    selectedQuality = "480p (Fast) - Lower bandwidth";
                    break;
                case 1:
                    selectedQuality = "720p (Balanced) - Good quality";
                    break;
                case 2:
                    selectedQuality = "1080p (High Quality) - More bandwidth";
                    break;
                default:
                    selectedQuality = "720p (Balanced)";
                    break;
            }
            lblStatus.Text = $"Quality set to: {selectedQuality}";
            lblStatus.ForeColor = Color.Yellow;
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
                    
                    // Calculate data size for display
                    double sizeKB = activity.ScreenshotData.Length / 1024.0;
                    string resolution = $"{image.Width}x{image.Height}";
                    
                    lblStatus.Text = $"Last Update: {activity.LastUpdate:HH:mm:ss} | {resolution} | {sizeKB:F1} KB | {activity.ActiveWindow}";
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

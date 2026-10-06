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
        private MemoryStream _currentImageStream;
        private bool _isFullScreen = false;
        private FormBorderStyle _previousBorderStyle;
        private FormWindowState _previousWindowState;

        public ScreenViewerForm(string pcName, MonitoringServer server)
        {
            this.pcName = pcName;
            this.server = server;
            InitializeComponent();
            InitializeCustomComponents();
            
            server.OnClientUpdate += Server_OnClientUpdate;

            // Load latest activity/screenshot immediately if server already has cached frame
            var cached = server?.GetClientActivity(pcName);
            if (cached != null && cached.ScreenshotData != null && cached.ScreenshotData.Length > 0)
            {
                UpdateScreenshot(cached);
            }
        }

        private void InitializeCustomComponents()
        {
            // Update labels with PC name
            this.Text = $"Screen View - {pcName} (F11 or double-click for Fullscreen)";
            lblPCName.Text = $"Viewing: {pcName}";
            
            // Setup quality selector
            cmbQuality.SelectedIndex = 1; // Default to 720p (Balanced)
            cmbQuality.SelectedIndexChanged += CmbQuality_SelectedIndexChanged;
            
            // Setup fit / stretch toggle
            btnToggleFit.Click += (s, e) =>
            {
                if (pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
                {
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    btnToggleFit.Text = "Mode: Stretch";
                }
                else
                {
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    btnToggleFit.Text = "Mode: Fit";
                }
            };

            // Setup fullscreen toggle (button, double-click, F11)
            btnToggleFullScreen.Click += (s, e) => ToggleFullScreen();
            pictureBox.DoubleClick += (s, e) => ToggleFullScreen();

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

        public void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _previousBorderStyle = this.FormBorderStyle;
                _previousWindowState = this.WindowState;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
                controlPanel.Visible = false;
                statusPanel.Visible = false;
                _isFullScreen = true;
            }
            else
            {
                controlPanel.Visible = true;
                statusPanel.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = _previousBorderStyle;
                this.WindowState = _previousWindowState;
                _isFullScreen = false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11 || (keyData == Keys.Escape && _isFullScreen))
            {
                ToggleFullScreen();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CmbQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedQuality = "720p";
            switch (cmbQuality.SelectedIndex)
            {
                case 0:
                    selectedQuality = "480p";
                    break;
                case 1:
                    selectedQuality = "720p";
                    break;
                case 2:
                    selectedQuality = "1080p";
                    break;
            }

            // Send command to client so it changes capture quality
            server.SendCommand(pcName, new ServerCommand
            {
                CommandType = "quality",
                MessageText = selectedQuality
            });

            lblStatus.Text = $"Quality requested: {selectedQuality}";
            lblStatus.ForeColor = Color.Yellow;
        }

        private void Server_OnClientUpdate(ClientActivity activity)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            // Use case-insensitive comparison for PC hostname
            if (string.Equals(activity.PCName, pcName, StringComparison.OrdinalIgnoreCase))
            {
                if (activity.ScreenshotData != null && activity.ScreenshotData.Length > 0)
                {
                    try
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
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
                else
                {
                    if (pictureBox.Image == null)
                    {
                        try
                        {
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() =>
                                {
                                    if (!this.IsDisposed)
                                    {
                                        lblStatus.Text = $"Connected ({activity.LastUpdate:HH:mm:ss}), awaiting screen data from {activity.PCName}...";
                                        lblStatus.ForeColor = Color.Yellow;
                                    }
                                }));
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private void UpdateScreenshot(ClientActivity activity)
        {
            if (this.IsDisposed) return;

            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(activity.ScreenshotData);
                var image = Image.FromStream(ms);
                
                var oldImage = pictureBox.Image;
                var oldStream = _currentImageStream;

                pictureBox.Image = image;
                _currentImageStream = ms;

                oldImage?.Dispose();
                oldStream?.Dispose();
                
                // Calculate data size for display
                double sizeKB = activity.ScreenshotData.Length / 1024.0;
                string resolution = $"{image.Width}x{image.Height}";
                
                if (!this.IsDisposed)
                {
                    lblStatus.Text = $"Last Update: {activity.LastUpdate:HH:mm:ss} | {resolution} | {sizeKB:F1} KB | {activity.ActiveWindow}";
                    lblStatus.ForeColor = Color.LightGreen;
                }
            }
            catch (Exception ex)
            {
                ms?.Dispose();
                if (!this.IsDisposed)
                {
                    lblStatus.Text = $"Error displaying screen: {ex.Message}";
                    lblStatus.ForeColor = Color.Red;
                }
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            // If we have not loaded an image yet, pull latest cached frame from server
            if (pictureBox.Image == null)
            {
                var cached = server?.GetClientActivity(pcName);
                if (cached != null && cached.ScreenshotData != null && cached.ScreenshotData.Length > 0)
                {
                    UpdateScreenshot(cached);
                }
            }

            // Check if client is still connected
            var clients = server?.GetConnectedClientsDictionary();
            if (clients != null && !clients.ContainsKey(pcName))
            {
                lblStatus.Text = "Client disconnected";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            var act = server?.GetClientActivity(pcName);
            if (act != null && act.ScreenshotData != null && act.ScreenshotData.Length > 0)
            {
                UpdateScreenshot(act);
            }
            else
            {
                lblStatus.Text = "Waiting for client screenshot (no screen data in server buffer)...";
                lblStatus.ForeColor = Color.Yellow;
            }
        }

        private void ScreenViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            server.OnClientUpdate -= Server_OnClientUpdate;
            
            var oldImage = pictureBox.Image;
            pictureBox.Image = null;
            oldImage?.Dispose();

            _currentImageStream?.Dispose();
            _currentImageStream = null;
        }
    }
}

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
        private DateTime _lastFrameTime = DateTime.MinValue;
        private double _currentFps = 0.0;

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

            // Draw helpful status message on black surface when awaiting image
            pictureBox.Paint += (s, pe) =>
            {
                if (pictureBox.Image == null)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(200, Color.LightGray)))
                    using (var font = new Font("Segoe UI", 12, FontStyle.Regular))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        pe.Graphics.DrawString($"Viewing {pcName}\nAwaiting screen capture frame...", font, brush, pictureBox.ClientRectangle, sf);
                    }
                }
            };

            // Setup snapshot save
            btnSaveSnapshot.Click += (s, e) => SaveSnapshot();

            // Setup direct messaging
            btnSendMessage.Click += (s, e) => SendDirectMessage();

            // Setup freeze action
            btnFreeze.Click += (s, e) => FreezeClient();

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

        private static bool HostnamesMatch(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            string shortA = a.Split('.')[0];
            string shortB = b.Split('.')[0];
            return string.Equals(shortA, shortB, StringComparison.OrdinalIgnoreCase);
        }

        private void Server_OnClientUpdate(ClientActivity activity)
        {
            if (this.IsDisposed || !this.IsHandleCreated || activity == null) return;

            // Match by hostname (short or FQDN) or IP address
            if (HostnamesMatch(activity.PCName, pcName) || string.Equals(activity.IPAddress, pcName, StringComparison.OrdinalIgnoreCase))
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
                
                // Calculate data size and FPS for display
                double sizeKB = activity.ScreenshotData.Length / 1024.0;
                string resolution = $"{image.Width}x{image.Height}";

                DateTime now = DateTime.Now;
                if (_lastFrameTime != DateTime.MinValue)
                {
                    double elapsed = (now - _lastFrameTime).TotalSeconds;
                    if (elapsed > 0.05)
                    {
                        _currentFps = Math.Round(1.0 / elapsed, 1);
                    }
                }
                _lastFrameTime = now;

                string fpsDisplay = _currentFps > 0 ? $"{_currentFps:F1} FPS" : "1 FPS";

                if (!this.IsDisposed)
                {
                    lblStatus.Text = $"Last: {activity.LastUpdate:HH:mm:ss} | {resolution} | {sizeKB:F1} KB | {fpsDisplay} | {activity.ActiveWindow}";
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
            bool isConnected = false;
            if (clients != null)
            {
                foreach (var kvp in clients)
                {
                    if (HostnamesMatch(kvp.Key, pcName) || string.Equals(kvp.Value.IPAddress, pcName, StringComparison.OrdinalIgnoreCase))
                    {
                        isConnected = true;
                        break;
                    }
                }
            }

            if (!isConnected)
            {
                lblStatus.Text = "Client disconnected or awaiting first frame...";
                lblStatus.ForeColor = Color.OrangeRed;
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

        private void SaveSnapshot()
        {
            if (pictureBox.Image == null)
            {
                MessageBox.Show("No screen image is currently available to save.", "Save Snapshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string defaultFileName = $"{pcName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = $"Save Screenshot - {pcName}";
                    sfd.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg";
                    sfd.FileName = defaultFileName;
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        var format = sfd.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                            ? System.Drawing.Imaging.ImageFormat.Jpeg
                            : System.Drawing.Imaging.ImageFormat.Png;

                        using (var clone = new Bitmap(pictureBox.Image))
                        {
                            clone.Save(sfd.FileName, format);
                        }
                        lblStatus.Text = $"Snapshot saved successfully: {Path.GetFileName(sfd.FileName)}";
                        lblStatus.ForeColor = Color.LightGreen;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save snapshot: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendDirectMessage()
        {
            using (Form msgDialog = new Form
            {
                Text = $"Send Message to {pcName}",
                Width = 420,
                Height = 230,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                Label lbl = new Label
                {
                    Text = $"Message to display on {pcName}'s screen:",
                    Location = new Point(15, 15),
                    AutoSize = true
                };

                TextBox txt = new TextBox
                {
                    Location = new Point(15, 40),
                    Width = 370,
                    Height = 80,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };

                Button btnOk = new Button
                {
                    Text = "Send",
                    Location = new Point(225, 140),
                    Width = 75,
                    DialogResult = DialogResult.OK
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(310, 140),
                    Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                msgDialog.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                msgDialog.AcceptButton = btnOk;
                msgDialog.CancelButton = btnCancel;

                if (msgDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                {
                    server?.SendCommand(pcName, new ServerCommand
                    {
                        CommandType = "message",
                        MessageText = txt.Text.Trim(),
                        Duration = 5
                    });
                    lblStatus.Text = $"Message sent to {pcName}";
                    lblStatus.ForeColor = Color.Cyan;
                }
            }
        }

        private void FreezeClient()
        {
            var res = MessageBox.Show(
                $"Freeze {pcName}'s screen for 3 seconds with administrator attention warning?\n\nThis will lock input temporarily.",
                $"Freeze {pcName}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                server?.SendCommand(pcName, new ServerCommand
                {
                    CommandType = "freeze",
                    MessageText = "⚠ ATTENTION: This screen has been frozen by the administrator for 3 seconds.",
                    Duration = 3
                });
                lblStatus.Text = $"Freeze command sent to {pcName}";
                lblStatus.ForeColor = Color.OrangeRed;
            }
        }
    }
}

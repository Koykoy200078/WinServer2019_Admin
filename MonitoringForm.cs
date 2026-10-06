using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinServer2019
{
    public partial class MonitoringForm : Form
    {
        private MonitoringServer monitoringServer;
        private System.Windows.Forms.Timer refreshTimer;

        // Cached fonts to prevent GDI handle leaks (Phase 1 fix)
        private readonly Font _fontHeaderBold = new Font("Segoe UI", 10, FontStyle.Bold);
        private readonly Font _fontBodyBold = new Font("Segoe UI", 9, FontStyle.Bold);
        private readonly Font _fontBody = new Font("Segoe UI", 9);
        private readonly Font _fontBodySmall = new Font("Segoe UI", 8);
        private readonly Font _fontConsolas = new Font("Consolas", 8);
        private readonly Font _fontConsolasBold = new Font("Consolas", 9, FontStyle.Bold);

        // O(1) cache for client list items to eliminate linear scans
        private readonly Dictionary<string, ListViewItem> _itemLookup = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);

        // Track open screen viewer windows to prevent duplicate windows per client
        private readonly Dictionary<string, ScreenViewerForm> _openScreenViewers = new Dictionary<string, ScreenViewerForm>(StringComparer.OrdinalIgnoreCase);

        // Real-time search filter query
        private string _currentFilter = "";

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, ref Point lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private const int WM_USER = 0x0400;
        private const int EM_GETSCROLLPOS = WM_USER + 221;
        private const int EM_SETSCROLLPOS = WM_USER + 222;
        private const int WM_SETREDRAW = 0x000B;

        public MonitoringForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            monitoringServer = new MonitoringServer();
            SetupEventHandlers();
        }

        // Custom comparer for PC name sorting (PC-1, PC-2, PC-10, PC-20, etc.)
        private class PCNameComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                ListViewItem itemX = x as ListViewItem;
                ListViewItem itemY = y as ListViewItem;

                if (itemX == null || itemY == null) return 0;

                string nameX = itemX.Text;
                string nameY = itemY.Text;

                // Extract number from PC-XX format
                var matchX = Regex.Match(nameX, @"PC-(\d+)", RegexOptions.IgnoreCase);
                var matchY = Regex.Match(nameY, @"PC-(\d+)", RegexOptions.IgnoreCase);

                if (matchX.Success && matchY.Success)
                {
                    int numX = int.Parse(matchX.Groups[1].Value);
                    int numY = int.Parse(matchY.Groups[1].Value);
                    return numX.CompareTo(numY);
                }

                // Fallback to string comparison
                return string.Compare(nameX, nameY);
            }
        }

        // Social media detection
        private readonly string[] socialMediaKeywords = new[]
        {
            "facebook", "twitter", "instagram", "tiktok", "snapchat",
            "whatsapp", "telegram", "discord", "reddit", "linkedin",
            "youtube", "twitch", "pinterest", "tumblr", "messenger"
        };

        private string DetectSocialMedia(string activeWindow)
        {
            if (string.IsNullOrEmpty(activeWindow)) return "";

            string lowerWindow = activeWindow.ToLower();
            foreach (var keyword in socialMediaKeywords)
            {
                if (lowerWindow.Contains(keyword))
                {
                    return $" 🔴 {keyword.ToUpper()}";
                }
            }
            return "";
        }

        private void InitializeCustomComponents()
        {
            // Add ListView columns
            lvClients.Columns.Add("PC Name", 120);
            lvClients.Columns.Add("Username", 120);
            lvClients.Columns.Add("Active Window", 350);
            lvClients.Columns.Add("Active Process", 150);
            lvClients.Columns.Add("CPU %", 70);
            lvClients.Columns.Add("Memory MB", 90);
            lvClients.Columns.Add("Last Update", 100);
            lvClients.Columns.Add("Actions", 150);

            // Enable sorting
            lvClients.ListViewItemSorter = new PCNameComparer();
            lvClients.Sorting = SortOrder.Ascending;

            // Enable double buffering to eliminate row flicker during updates
            try
            {
                typeof(ListView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, lvClients, new object[] { true });
            }
            catch { }

            // Refresh Timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 3000; // 3s — clients send every 2s anyway
            refreshTimer.Tick += RefreshTimer_Tick;

            // Context menu for client actions
            SetupContextMenu();

            // Setup broadcast buttons
            btnBroadcastMsg.Click += BtnBroadcastMsg_Click;
            btnBroadcastFreeze.Click += BtnBroadcastFreeze_Click;

            // Setup real-time search / filter
            txtFilter.TextChanged += TxtFilter_TextChanged;

            // Initial counter update
            UpdateOnlineCount();
        }

        private void SetupContextMenu()
        {
            var cms = new ContextMenuStrip();

            var miViewScreen = new ToolStripMenuItem("👁 View Screen");
            miViewScreen.Click += (s, e) =>
            {
                if (lvClients.SelectedItems.Count > 0 && lvClients.SelectedItems[0].Tag is ClientActivity act)
                {
                    OpenScreenViewer(act);
                }
            };

            var miSendMessage = new ToolStripMenuItem("📨 Send Message");
            miSendMessage.Click += (s, e) =>
            {
                if (lvClients.SelectedItems.Count > 0 && lvClients.SelectedItems[0].Tag is ClientActivity act)
                {
                    SendMessageToClient(act);
                }
            };

            var miFreezeScreen = new ToolStripMenuItem("⚠ Freeze Screen");
            miFreezeScreen.Click += (s, e) =>
            {
                if (lvClients.SelectedItems.Count > 0 && lvClients.SelectedItems[0].Tag is ClientActivity act)
                {
                    FreezeClientScreen(act);
                }
            };

            var miCopyDetails = new ToolStripMenuItem("📋 Copy Client Info");
            miCopyDetails.Click += (s, e) =>
            {
                if (lvClients.SelectedItems.Count > 0 && lvClients.SelectedItems[0].Tag is ClientActivity act)
                {
                    string info = $"PC: {act.PCName}\nUser: {act.Username}\nIP: {act.IPAddress}\nWindow: {act.ActiveWindow}\nProcess: {act.ActiveProcess}\nCPU: {act.CPUUsage:F1}%\nRAM: {act.MemoryUsageMB:F0} MB";
                    try { Clipboard.SetText(info); } catch { }
                }
            };

            cms.Items.AddRange(new ToolStripItem[] {
                miViewScreen,
                miSendMessage,
                miFreezeScreen,
                new ToolStripSeparator(),
                miCopyDetails
            });

            lvClients.ContextMenuStrip = cms;
        }

        private void SetupEventHandlers()
        {
            monitoringServer.OnLogMessage += (msg) =>
            {
                AppendLog(msg, Color.Cyan);
            };

            monitoringServer.OnClientUpdate += (activity) =>
            {
                UpdateClientList(activity);
            };

            monitoringServer.OnClientDisconnected += (clientId) =>
            {
                RemoveClientFromList(clientId);
                AppendLog($"Client disconnected: {clientId}", Color.Orange);
            };
        }

        private void UpdateOnlineCount()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(UpdateOnlineCount));
                }
                catch { }
                return;
            }

            int totalCount = _itemLookup.Count;
            int visibleCount = lvClients.Items.Count;
            if (string.IsNullOrWhiteSpace(_currentFilter))
            {
                lblTotalOnline.Text = $"Total Online: {totalCount} / 35";
            }
            else
            {
                lblTotalOnline.Text = $"Filtered: {visibleCount} / {totalCount} Online";
            }

            if (totalCount >= 30)
            {
                lblTotalOnline.ForeColor = Color.ForestGreen;
            }
            else if (totalCount >= 15)
            {
                lblTotalOnline.ForeColor = Color.DarkOrange;
            }
            else
            {
                lblTotalOnline.ForeColor = Color.Red;
            }

            this.Text = $"Real-Time PC Monitoring — {totalCount}/35 Online";
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStartStop.Text.Contains("Start"))
                {
                    monitoringServer.Start();
                    btnStartStop.Text = "Stop Monitoring Server";
                    btnStartStop.BackColor = Color.FromArgb(192, 0, 0);
                    lblStatus.Text = "Server Status: Running";
                    lblStatus.ForeColor = Color.Green;
                    refreshTimer.Start();
                    UpdateOnlineCount();
                }
                else
                {
                    monitoringServer.Stop();
                    btnStartStop.Text = "Start Monitoring Server";
                    btnStartStop.BackColor = Color.FromArgb(0, 120, 215);
                    lblStatus.Text = "Server Status: Stopped";
                    lblStatus.ForeColor = Color.Red;
                    refreshTimer.Stop();
                    lvClients.Items.Clear();
                    _itemLookup.Clear();
                    UpdateOnlineCount();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => RefreshTimer_Tick(sender, e)));
                }
                catch { }
                return;
            }

            var clients = monitoringServer.GetConnectedClientsDictionary();

            lvClients.BeginUpdate();
            try
            {
                foreach (var kvp in _itemLookup)
                {
                    if (clients.TryGetValue(kvp.Key, out var client))
                    {
                        UpdateListViewItem(kvp.Value, client);
                    }
                }
            }
            finally
            {
                lvClients.EndUpdate();
            }
        }

        private static bool HostnamesMatch(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            string shortA = a.Split('.')[0];
            string shortB = b.Split('.')[0];
            return string.Equals(shortA, shortB, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateClientList(ClientActivity activity)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => UpdateClientList(activity)));
                }
                catch { }
                return;
            }

            if (chkVerboseLog.Checked)
            {
                AppendLog($"Update from {activity.PCName}: {activity.ActiveWindow}", Color.White);
            }

            ListViewItem existingItem = null;
            if (_itemLookup.TryGetValue(activity.PCName, out existingItem))
            {
                // Exact key match
            }
            else
            {
                string shortName = activity.PCName.Split('.')[0];
                foreach (var kvp in _itemLookup)
                {
                    if (string.Equals(kvp.Key.Split('.')[0], shortName, StringComparison.OrdinalIgnoreCase))
                    {
                        existingItem = kvp.Value;
                        break;
                    }
                }
            }

            if (existingItem != null)
            {
                UpdateListViewItem(existingItem, activity);
            }
            else
            {
                var item = new ListViewItem(activity.PCName);
                item.SubItems.Add(activity.Username);
                item.SubItems.Add(activity.ActiveWindow);
                item.SubItems.Add(activity.ActiveProcess);
                item.SubItems.Add(activity.CPUUsage.ToString("F1"));
                item.SubItems.Add(activity.MemoryUsageMB.ToString("F0"));
                item.SubItems.Add(activity.LastUpdate.ToString("HH:mm:ss"));
                item.SubItems.Add("👁 View | 📨 Msg");
                item.Tag = activity;

                _itemLookup[activity.PCName] = item;
                if (MatchesFilter(activity))
                {
                    // Temporarily detach sorter during add to avoid per-item sort overhead
                    var currentSorter = lvClients.ListViewItemSorter;
                    lvClients.ListViewItemSorter = null;
                    try
                    {
                        lvClients.Items.Add(item);
                    }
                    finally
                    {
                        lvClients.ListViewItemSorter = currentSorter;
                        lvClients.Sort();
                    }
                }

                UpdateOnlineCount();
                AppendLog($"Client connected: {activity.PCName} ({activity.IPAddress})", Color.LimeGreen);
            }
        }

        private void UpdateListViewItem(ListViewItem item, ClientActivity activity)
        {
            item.SubItems[1].Text = activity.Username;
            
            // Add social media tag to Active Window
            string socialMediaTag = DetectSocialMedia(activity.ActiveWindow);
            item.SubItems[2].Text = activity.ActiveWindow + socialMediaTag;
            if (!string.IsNullOrEmpty(socialMediaTag))
            {
                item.SubItems[2].ForeColor = Color.Red;
                item.SubItems[2].Font = _fontConsolasBold;
            }
            else
            {
                item.SubItems[2].ForeColor = Color.Black;
                item.SubItems[2].Font = lvClients.Font;
            }
            
            item.SubItems[3].Text = activity.ActiveProcess;
            item.SubItems[4].Text = activity.CPUUsage.ToString("F1");
            item.SubItems[5].Text = activity.MemoryUsageMB.ToString("F0");
            item.SubItems[6].Text = activity.LastUpdate.ToString("HH:mm:ss");
            item.SubItems[7].Text = "👁 View | 📨 Msg";
            item.Tag = activity;

            // Manage visibility based on filter
            if (MatchesFilter(activity))
            {
                if (!lvClients.Items.Contains(item))
                {
                    lvClients.Items.Add(item);
                }
            }
            else
            {
                if (lvClients.Items.Contains(item))
                {
                    lvClients.Items.Remove(item);
                }
            }

            // Highlight based on client health / time since last update
            var timeSinceUpdate = (DateTime.Now - activity.LastUpdate).TotalSeconds;
            if (timeSinceUpdate < 5)
            {
                item.BackColor = Color.FromArgb(240, 255, 240); // Soft green: online & active
            }
            else if (timeSinceUpdate < 15)
            {
                item.BackColor = Color.FromArgb(255, 255, 230); // Soft yellow: slight delay
            }
            else
            {
                item.BackColor = Color.FromArgb(255, 230, 230); // Soft red: lagging / disconnected soon
            }

            // Update details sidebar live if this client is currently selected
            if (lvClients.SelectedItems.Count > 0 && lvClients.SelectedItems[0] == item)
            {
                DisplayClientDetails(activity);
            }
        }

        private void RemoveClientFromList(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return;
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => RemoveClientFromList(clientId)));
                }
                catch { }
                return;
            }

            ListViewItem item = null;
            string matchedKey = null;

            if (_itemLookup.TryGetValue(clientId, out item))
            {
                matchedKey = clientId;
            }
            else
            {
                string shortId = clientId.Split('.')[0];
                foreach (var kvp in _itemLookup)
                {
                    if (string.Equals(kvp.Key.Split('.')[0], shortId, StringComparison.OrdinalIgnoreCase))
                    {
                        item = kvp.Value;
                        matchedKey = kvp.Key;
                        break;
                    }
                }
            }

            if (item != null && matchedKey != null)
            {
                _itemLookup.Remove(matchedKey);
                lvClients.Items.Remove(item);
                UpdateOnlineCount();
            }
        }

        private void LvClients_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var activity = lvClients.SelectedItems[0].Tag as ClientActivity;
            if (activity == null) return;

            OpenScreenViewer(activity);
        }

        private void OpenScreenViewer(ClientActivity activity)
        {
            ScreenViewerForm existingViewer = null;
            string existingKey = null;

            foreach (var kvp in _openScreenViewers)
            {
                if (HostnamesMatch(kvp.Key, activity.PCName))
                {
                    existingViewer = kvp.Value;
                    existingKey = kvp.Key;
                    break;
                }
            }

            if (existingViewer != null && !existingViewer.IsDisposed)
            {
                if (existingViewer.WindowState == FormWindowState.Minimized)
                {
                    existingViewer.WindowState = FormWindowState.Normal;
                }
                existingViewer.BringToFront();
                existingViewer.Activate();
                return;
            }

            var screenViewer = new ScreenViewerForm(activity.PCName, monitoringServer);
            _openScreenViewers[activity.PCName] = screenViewer;
            screenViewer.FormClosed += (s, e) =>
            {
                _openScreenViewers.Remove(activity.PCName);
                if (existingKey != null) _openScreenViewers.Remove(existingKey);
            };
            screenViewer.Show();
        }

        private void SendMessageToClient(ClientActivity activity)
        {
            // Create input dialog
            using (Form messageDialog = new Form
            {
                Text = $"Send Message to {activity.PCName}",
                Width = 450,
                Height = 250,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                Label lblMessage = new Label
                {
                    Text = "Enter message to display on client screen:",
                    Location = new Point(15, 15),
                    AutoSize = true
                };

                TextBox txtMessage = new TextBox
                {
                    Location = new Point(15, 40),
                    Width = 400,
                    Height = 100,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };

                Button btnSend = new Button
                {
                    Text = "Send Message",
                    Location = new Point(240, 160),
                    Width = 100,
                    DialogResult = DialogResult.OK
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(345, 160),
                    Width = 70,
                    DialogResult = DialogResult.Cancel
                };

                messageDialog.Controls.AddRange(new Control[] { lblMessage, txtMessage, btnSend, btnCancel });
                messageDialog.AcceptButton = btnSend;
                messageDialog.CancelButton = btnCancel;

                if (messageDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtMessage.Text))
                {
                    string message = txtMessage.Text.Trim();
                    var command = new ServerCommand
                    {
                        CommandType = "message",
                        MessageText = message,
                        Duration = 5 // Display for 5 seconds
                    };
                    monitoringServer.SendCommand(activity.PCName, command);
                    MessageBox.Show($"Message queued for {activity.PCName}", 
                                   "Message Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void FreezeClientScreen(ClientActivity activity)
        {
            DialogResult result = MessageBox.Show(
                $"This will freeze {activity.PCName}'s screen for 3 seconds with a warning message.\n\nContinue?",
                "Freeze Screen Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var command = new ServerCommand
                {
                    CommandType = "freeze",
                    MessageText = "⚠ ATTENTION: This screen has been frozen by the administrator for 3 seconds.",
                    Duration = 3
                };
                monitoringServer.SendCommand(activity.PCName, command);
                MessageBox.Show($"Freeze command queued for {activity.PCName}", 
                               "Command Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LvClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var activity = lvClients.SelectedItems[0].Tag as ClientActivity;
            if (activity == null) return;

            DisplayClientDetails(activity);
        }

        private void DisplayClientDetails(ClientActivity activity)
        {
            if (activity == null || this.IsDisposed) return;

            Point scrollPoint = new Point();
            bool hasHandle = rtbClientDetails.IsHandleCreated;
            if (hasHandle)
            {
                SendMessage(rtbClientDetails.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref scrollPoint);
                SendMessage(rtbClientDetails.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }

            try
            {
                rtbClientDetails.Clear();
                rtbClientDetails.SelectionFont = _fontHeaderBold;
                rtbClientDetails.SelectionColor = Color.DarkBlue;
                rtbClientDetails.AppendText($" 📊 {activity.PCName} - Details\n");
                rtbClientDetails.AppendText(new string('─', 40) + "\n\n");

                rtbClientDetails.SelectionFont = _fontBodyBold;
                rtbClientDetails.SelectionColor = Color.Black;
                rtbClientDetails.AppendText(" User: ");
                rtbClientDetails.SelectionFont = _fontBody;
                rtbClientDetails.AppendText($" {activity.Username}\n");

                rtbClientDetails.SelectionFont = _fontBodyBold;
                rtbClientDetails.AppendText(" IP Address: ");
                rtbClientDetails.SelectionFont = _fontBody;
                rtbClientDetails.AppendText($" {activity.IPAddress}\n\n");

                rtbClientDetails.SelectionFont = _fontBodyBold;
                rtbClientDetails.SelectionColor = Color.DarkGreen;
                rtbClientDetails.AppendText(" 🖥️ Current Activity:\n");
                rtbClientDetails.SelectionFont = _fontBody;
                rtbClientDetails.SelectionColor = Color.Black;
                rtbClientDetails.AppendText($" Window: {activity.ActiveWindow}\n");
                rtbClientDetails.AppendText($" Process: {activity.ActiveProcess}\n\n");

                rtbClientDetails.SelectionFont = _fontBodyBold;
                rtbClientDetails.SelectionColor = Color.DarkOrange;
                rtbClientDetails.AppendText(" 💻 System Resources:\n");
                rtbClientDetails.SelectionFont = _fontBody;
                rtbClientDetails.SelectionColor = Color.Black;
                rtbClientDetails.AppendText($" CPU Usage: {activity.CPUUsage:F1}%\n");
                rtbClientDetails.AppendText($" Memory Usage: {activity.MemoryUsageMB:F0} MB\n\n");

                if (activity.RunningProcesses != null && activity.RunningProcesses.Any())
                {
                    rtbClientDetails.SelectionFont = _fontBodyBold;
                    rtbClientDetails.SelectionColor = Color.DarkRed;
                    rtbClientDetails.AppendText(" 🔄 Top Running Processes:\n");
                    rtbClientDetails.SelectionFont = _fontConsolas;
                    rtbClientDetails.SelectionColor = Color.Black;
                    foreach (var proc in activity.RunningProcesses.Take(10))
                    {
                        rtbClientDetails.AppendText($"  • {proc}\n");
                    }
                    rtbClientDetails.AppendText("\n");
                }

                if (activity.RecentActivities != null && activity.RecentActivities.Any())
                {
                    rtbClientDetails.SelectionFont = _fontBodyBold;
                    rtbClientDetails.SelectionColor = Color.DarkMagenta;
                    rtbClientDetails.AppendText("📝 Recent Activities:\n");
                    rtbClientDetails.SelectionFont = _fontBodySmall;
                    rtbClientDetails.SelectionColor = Color.DarkGray;
                    foreach (var act in activity.RecentActivities.Take(15))
                    {
                        rtbClientDetails.AppendText($"  {act}\n");
                    }
                }
            }
            finally
            {
                if (hasHandle && !rtbClientDetails.IsDisposed)
                {
                    SendMessage(rtbClientDetails.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    SendMessage(rtbClientDetails.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref scrollPoint);
                    rtbClientDetails.Invalidate();
                }
            }
        }

        private void AppendLog(string message, Color color)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => AppendLog(message, color)));
                }
                catch { }
                return;
            }

            // Cap log at 1000 lines to prevent memory/performance degradation
            if (rtbActivityLog.Lines.Length > 1000)
            {
                rtbActivityLog.SuspendLayout();
                int cutIndex = rtbActivityLog.GetFirstCharIndexFromLine(500);
                rtbActivityLog.Select(0, cutIndex);
                rtbActivityLog.SelectedText = "";
                rtbActivityLog.ResumeLayout();
            }

            rtbActivityLog.SelectionStart = rtbActivityLog.TextLength;
            rtbActivityLog.SelectionLength = 0;
            rtbActivityLog.SelectionColor = color;
            rtbActivityLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            rtbActivityLog.ScrollToCaret();
        }

        private void BtnExportLog_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log";
                    sfd.FileName = $"ActivityLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, rtbActivityLog.Text);
                        MessageBox.Show("Activity log exported successfully!", "Export Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export log: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            rtbActivityLog.Clear();
        }

        private void MonitoringForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var viewer in _openScreenViewers.Values.ToList())
            {
                try
                {
                    if (!viewer.IsDisposed) viewer.Close();
                }
                catch { }
            }
            _openScreenViewers.Clear();

            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            monitoringServer?.Stop();
            monitoringServer?.Dispose();
        }

        private bool MatchesFilter(ClientActivity activity)
        {
            if (string.IsNullOrWhiteSpace(_currentFilter)) return true;
            if (activity == null) return false;

            return (activity.PCName != null && activity.PCName.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (activity.Username != null && activity.Username.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (activity.ActiveWindow != null && activity.ActiveWindow.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (activity.ActiveProcess != null && activity.ActiveProcess.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (activity.IPAddress != null && activity.IPAddress.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            _currentFilter = txtFilter.Text.Trim();
            ApplyFilter();
            UpdateOnlineCount();
        }

        private void ApplyFilter()
        {
            lvClients.BeginUpdate();
            try
            {
                lvClients.Items.Clear();
                foreach (var kvp in _itemLookup)
                {
                    if (kvp.Value.Tag is ClientActivity act && MatchesFilter(act))
                    {
                        lvClients.Items.Add(kvp.Value);
                    }
                }
                lvClients.Sort();
            }
            finally
            {
                lvClients.EndUpdate();
            }
        }

        private void BtnBroadcastMsg_Click(object sender, EventArgs e)
        {
            int connected = monitoringServer?.GetConnectedClients().Count ?? 0;
            if (connected == 0)
            {
                MessageBox.Show("No workstations are currently connected.", "Broadcast Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (Form msgDialog = new Form
            {
                Text = $"Broadcast Message to All Workstations ({connected} Online)",
                Width = 480,
                Height = 260,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                Label lbl = new Label
                {
                    Text = $"Enter message to display on ALL {connected} student screens:",
                    Location = new Point(15, 15),
                    AutoSize = true
                };

                TextBox txt = new TextBox
                {
                    Location = new Point(15, 40),
                    Width = 430,
                    Height = 100,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };

                Button btnSend = new Button
                {
                    Text = "📢 Broadcast",
                    Location = new Point(245, 160),
                    Width = 110,
                    DialogResult = DialogResult.OK
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(365, 160),
                    Width = 80,
                    DialogResult = DialogResult.Cancel
                };

                msgDialog.Controls.AddRange(new Control[] { lbl, txt, btnSend, btnCancel });
                msgDialog.AcceptButton = btnSend;
                msgDialog.CancelButton = btnCancel;

                if (msgDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
                {
                    string message = txt.Text.Trim();
                    var command = new ServerCommand
                    {
                        CommandType = "message",
                        MessageText = message,
                        Duration = 5
                    };
                    int sent = monitoringServer.BroadcastCommand(command);
                    AppendLog($"📢 Broadcast message sent to {sent} workstations: \"{message}\"", Color.Cyan);
                    MessageBox.Show($"Message successfully broadcasted to {sent} workstations.", "Broadcast Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnBroadcastFreeze_Click(object sender, EventArgs e)
        {
            int connected = monitoringServer?.GetConnectedClients().Count ?? 0;
            if (connected == 0)
            {
                MessageBox.Show("No workstations are currently connected.", "Broadcast Freeze", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"⚠ WARNING: This will FREEZE all {connected} connected workstations for 3 seconds with an attention warning.\n\nAre you sure you want to proceed?",
                "Confirm Lab-wide Screen Freeze",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var command = new ServerCommand
                {
                    CommandType = "freeze",
                    MessageText = "⚠ ATTENTION: All lab workstations have been temporarily frozen by the instructor.",
                    Duration = 3
                };
                int sent = monitoringServer.BroadcastCommand(command);
                AppendLog($"🔒 Lab-wide screen freeze broadcasted to {sent} workstations", Color.OrangeRed);
                MessageBox.Show($"Freeze command successfully sent to {sent} workstations.", "Freeze Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

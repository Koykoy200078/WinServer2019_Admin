using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinServer2019
{
    public partial class MonitoringForm : Form
    {
        private MonitoringServer monitoringServer;
        private System.Windows.Forms.Timer refreshTimer;

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
            lvClients.Columns.Add("Active Window", 250);
            lvClients.Columns.Add("Active Process", 150);
            lvClients.Columns.Add("CPU %", 70);
            lvClients.Columns.Add("Memory MB", 90);
            lvClients.Columns.Add("Last Update", 100);
            lvClients.Columns.Add("Actions", 150);

            // Enable sorting
            lvClients.ListViewItemSorter = new PCNameComparer();
            lvClients.Sorting = SortOrder.Ascending;

            // Refresh Timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 1000;
            refreshTimer.Tick += RefreshTimer_Tick;
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
                AppendLog($"Update from {activity.PCName}: {activity.ActiveWindow}", Color.White);
            };

            monitoringServer.OnClientDisconnected += (clientId) =>
            {
                RemoveClientFromList(clientId);
                AppendLog($"Client disconnected: {clientId}", Color.Orange);
            };
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => RefreshTimer_Tick(sender, e)));
                return;
            }

            var clients = monitoringServer.GetConnectedClients();
            
            // Update existing items
            foreach (ListViewItem item in lvClients.Items)
            {
                var client = clients.FirstOrDefault(c => c.PCName == item.Text);
                if (client != null)
                {
                    UpdateListViewItem(item, client);
                }
            }
        }

        private void UpdateClientList(ClientActivity activity)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateClientList(activity)));
                return;
            }

            var existingItem = lvClients.Items.Cast<ListViewItem>()
                .FirstOrDefault(i => i.Text == activity.PCName);

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
                item.SubItems.Add("👁 View");
                item.Tag = activity;
                lvClients.Items.Add(item);
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
                item.SubItems[2].Font = new Font(item.SubItems[2].Font, FontStyle.Bold);
            }
            
            item.SubItems[3].Text = activity.ActiveProcess;
            item.SubItems[4].Text = activity.CPUUsage.ToString("F1");
            item.SubItems[5].Text = activity.MemoryUsageMB.ToString("F0");
            item.SubItems[6].Text = activity.LastUpdate.ToString("HH:mm:ss");
            item.SubItems[7].Text = "👁 View | 📨 Msg | ⚠ Warn";
            item.Tag = activity;

            // Highlight if recently updated
            var timeSinceUpdate = (DateTime.Now - activity.LastUpdate).TotalSeconds;
            item.BackColor = timeSinceUpdate < 2 ? Color.LightGreen : Color.White;
        }

        private void RemoveClientFromList(string clientId)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => RemoveClientFromList(clientId)));
                return;
            }

            var item = lvClients.Items.Cast<ListViewItem>()
                .FirstOrDefault(i => i.Text == clientId);
            
            if (item != null)
            {
                lvClients.Items.Remove(item);
            }
        }

        private void LvClients_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var activity = lvClients.SelectedItems[0].Tag as ClientActivity;
            if (activity == null) return;

            // Check which column was clicked
            ListViewItem.ListViewSubItem clickedSubItem = lvClients.SelectedItems[0].GetSubItemAt(e.X, e.Y);
            int columnIndex = lvClients.SelectedItems[0].SubItems.IndexOf(clickedSubItem);

            if (columnIndex == 7) // Actions column
            {
                // Determine which action based on X position
                int relativeX = e.X - lvClients.Columns[0].Width - lvClients.Columns[1].Width - 
                                lvClients.Columns[2].Width - lvClients.Columns[3].Width - 
                                lvClients.Columns[4].Width - lvClients.Columns[5].Width - 
                                lvClients.Columns[6].Width;

                if (relativeX < 50) // View icon
                {
                    OpenScreenViewer(activity);
                }
                else if (relativeX >= 50 && relativeX < 100) // Message icon
                {
                    SendMessageToClient(activity);
                }
                else if (relativeX >= 100) // Warn/Freeze icon
                {
                    FreezeClientScreen(activity);
                }
            }
            else
            {
                // Default: open screen viewer on any other column
                OpenScreenViewer(activity);
            }
        }

        private void OpenScreenViewer(ClientActivity activity)
        {
            var screenViewer = new ScreenViewerForm(activity.PCName, monitoringServer);
            screenViewer.Show();
        }

        private void SendMessageToClient(ClientActivity activity)
        {
            // Create input dialog
            Form messageDialog = new Form
            {
                Text = $"Send Message to {activity.PCName}",
                Width = 450,
                Height = 250,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

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
                // TODO: Send command to server to forward to client
                MessageBox.Show($"Message will be sent to {activity.PCName}:\n\n{message}\n\n(Command sending not yet implemented)", 
                               "Message Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                // TODO: Send freeze command to server to forward to client
                MessageBox.Show($"Freeze command will be sent to {activity.PCName}\n\n(Command sending not yet implemented)", 
                               "Command Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LvClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvClients.SelectedItems.Count == 0) return;

            var activity = lvClients.SelectedItems[0].Tag as ClientActivity;
            if (activity == null) return;

            rtbClientDetails.Clear();
            rtbClientDetails.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            rtbClientDetails.SelectionColor = Color.DarkBlue;
            rtbClientDetails.AppendText($" 📊 {activity.PCName} - Details\n");
            rtbClientDetails.AppendText(new string('─', 40) + "\n\n");

            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
            rtbClientDetails.SelectionColor = Color.Black;
            rtbClientDetails.AppendText(" User: ");
            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9);
            rtbClientDetails.AppendText($" {activity.Username}\n");

            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
            rtbClientDetails.AppendText(" IP Address: ");
            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9);
            rtbClientDetails.AppendText($" {activity.IPAddress}\n\n");

            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
            rtbClientDetails.SelectionColor = Color.DarkGreen;
            rtbClientDetails.AppendText(" 🖥️ Current Activity:\n");
            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9);
            rtbClientDetails.SelectionColor = Color.Black;
            rtbClientDetails.AppendText($" Window: {activity.ActiveWindow}\n");
            rtbClientDetails.AppendText($" Process: {activity.ActiveProcess}\n\n");

            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
            rtbClientDetails.SelectionColor = Color.DarkOrange;
            rtbClientDetails.AppendText(" 💻 System Resources:\n");
            rtbClientDetails.SelectionFont = new Font("Segoe UI", 9);
            rtbClientDetails.SelectionColor = Color.Black;
            rtbClientDetails.AppendText($" CPU Usage: {activity.CPUUsage:F1}%\n");
            rtbClientDetails.AppendText($" Memory Usage: {activity.MemoryUsageMB:F0} MB\n\n");

            if (activity.RunningProcesses != null && activity.RunningProcesses.Any())
            {
                rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
                rtbClientDetails.SelectionColor = Color.DarkRed;
                rtbClientDetails.AppendText(" 🔄 Top Running Processes:\n");
                rtbClientDetails.SelectionFont = new Font("Consolas", 8);
                rtbClientDetails.SelectionColor = Color.Black;
                foreach (var proc in activity.RunningProcesses.Take(10))
                {
                    rtbClientDetails.AppendText($"  • {proc}\n");
                }
                rtbClientDetails.AppendText("\n");
            }

            if (activity.RecentActivities != null && activity.RecentActivities.Any())
            {
                rtbClientDetails.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
                rtbClientDetails.SelectionColor = Color.DarkMagenta;
                rtbClientDetails.AppendText("📝 Recent Activities:\n");
                rtbClientDetails.SelectionFont = new Font("Segoe UI", 8);
                rtbClientDetails.SelectionColor = Color.DarkGray;
                foreach (var act in activity.RecentActivities.Take(15))
                {
                    rtbClientDetails.AppendText($"  {act}\n");
                }
            }
        }

        private void AppendLog(string message, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendLog(message, color)));
                return;
            }

            rtbActivityLog.SelectionStart = rtbActivityLog.TextLength;
            rtbActivityLog.SelectionLength = 0;
            rtbActivityLog.SelectionColor = color;
            rtbActivityLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            rtbActivityLog.ScrollToCaret();
        }

        private void MonitoringForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            monitoringServer?.Stop();
            monitoringServer?.Dispose();
        }

        private void lblLog_Click(object sender, EventArgs e)
        {

        }

        private void rtbClientDetails_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

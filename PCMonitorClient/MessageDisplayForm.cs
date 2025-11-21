using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PCMonitorClient
{
    public class MessageDisplayForm : Form
    {
        private Label lblMessage;
        private Timer closeTimer;
        private int duration;

        // P/Invoke to block input
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);

        public MessageDisplayForm(string message, int durationSeconds)
        {
            duration = durationSeconds;
            InitializeComponent(message);
        }

        private void InitializeComponent(string message)
        {
            // Form settings - fullscreen overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.90;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.Cursor = Cursors.No;

            // Message label - centered
            lblMessage = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = message,
                Dock = DockStyle.Fill
            };

            this.Controls.Add(lblMessage);

            // Auto-close timer
            closeTimer = new Timer
            {
                Interval = duration * 1000
            };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer?.Stop();
                BlockInput(false); // Unblock input
                this.Close();
            };

            // Block keyboard/mouse input
            this.Load += (s, e) =>
            {
                BlockInput(true);
                closeTimer.Start();
            };

            this.FormClosing += (s, e) =>
            {
                BlockInput(false); // Ensure input is unblocked
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer?.Stop();
                closeTimer?.Dispose();
                BlockInput(false); // Ensure input is unblocked
            }
            base.Dispose(disposing);
        }

        // Prevent Alt+F4 and other key combinations
        protected override bool ProcessDialogKey(Keys keyData)
        {
            return true; // Block all keys
        }
    }
}

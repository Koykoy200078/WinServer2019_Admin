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
        private bool blockInput;
        private Font _messageFont;

        // P/Invoke to block input
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);

        public MessageDisplayForm(string message, int durationSeconds)
            : this(message, durationSeconds, false)
        {
        }

        public MessageDisplayForm(string message, int durationSeconds, bool blockInput)
        {
            this.duration = Math.Max(1, durationSeconds);
            this.blockInput = blockInput;
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
            this.Cursor = blockInput ? Cursors.No : Cursors.Default;

            // Message label - centered
            _messageFont = new Font("Segoe UI", 36, FontStyle.Bold);
            lblMessage = new Label
            {
                AutoSize = false,
                Font = _messageFont,
                ForeColor = blockInput ? Color.Red : Color.Yellow,
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
                if (blockInput) BlockInput(false);
                this.Close();
            };

            // Block keyboard/mouse input if requested
            this.Load += (s, e) =>
            {
                if (blockInput)
                {
                    BlockInput(true);
                }
                closeTimer.Start();
            };

            this.FormClosing += (s, e) =>
            {
                if (blockInput)
                {
                    BlockInput(false); // Ensure input is unblocked
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer?.Stop();
                closeTimer?.Dispose();
                _messageFont?.Dispose();
                if (blockInput)
                {
                    BlockInput(false);
                }
            }
            base.Dispose(disposing);
        }

        // Handle keys
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (!blockInput)
            {
                // For normal messages, allow student to dismiss with Enter, Escape, or Space
                if (keyData == Keys.Escape || keyData == Keys.Enter || keyData == Keys.Space)
                {
                    this.Close();
                    return true;
                }
            }
            return true; // Block other keys during overlay
        }
    }
}

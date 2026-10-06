using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PCMonitorClient
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("shcore.dll", SetLastError = true)]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        private static void EnableDpiAwareness()
        {
            try
            {
                // Windows 10 1607+ / Windows Server 2016/2019+ Per-Monitor V2
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                    return;
            }
            catch { }

            try
            {
                // Windows 8.1+ Per-Monitor
                if (SetProcessDpiAwareness(2) == 0)
                    return;
            }
            catch { }

            try
            {
                // Windows Vista/7 System DPI
                SetProcessDPIAware();
            }
            catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Enable DPI awareness so Screen.PrimaryScreen.Bounds returns true physical resolution
            // and CopyFromScreen captures the complete screen without cropping or scaling cuts
            EnableDpiAwareness();

            // Check if already running
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "PCMonitorClient_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    // Already running, exit silently
                    return;
                }

                // Read server IP from args or use default
                string serverIP = args.Length > 0 ? args[0] : "192.168.2.45";
                int serverPort = 8888;
                if (args.Length > 1)
                {
                    if (!int.TryParse(args[1], out serverPort) || serverPort <= 0 || serverPort > 65535)
                    {
                        serverPort = 8888;
                    }
                }

                // Start monitoring in background
                var client = new MonitoringClient(serverIP, serverPort);
                client.Start();

                // Cleanly dispose client on application exit
                Application.ApplicationExit += (s, e) =>
                {
                    try
                    {
                        client?.Dispose();
                    }
                    catch { }
                };

                // Keep application running (hidden)
                Application.Run(new HiddenForm());
            }
        }

        // Hidden form to keep application running
        private class HiddenForm : Form
        {
            public HiddenForm()
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Opacity = 0;
                this.FormBorderStyle = FormBorderStyle.None;
            }

            protected override void SetVisibleCore(bool value)
            {
                base.SetVisibleCore(false);
            }
        }
    }
}

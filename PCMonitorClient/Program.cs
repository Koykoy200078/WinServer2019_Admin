using System;
using System.Windows.Forms;

namespace PCMonitorClient
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
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
                int serverPort = args.Length > 1 ? int.Parse(args[1]) : 8888;

                // Start monitoring in background
                var client = new MonitoringClient(serverIP, serverPort);
                client.Start();

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinServer2019
{
    internal static class Program
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

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            EnableDpiAwareness();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Show login form first
            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // Login successful, open main activity with credentials
                    Application.Run(new MainActivity(
                        loginForm.Username,
                        loginForm.Password,
                        loginForm.Domain
                    ));
                }
                else
                {
                    // Login cancelled or failed
                    MessageBox.Show("Login cancelled. Application will exit.", 
                        "PC Management System", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
            }
        }
    }
}

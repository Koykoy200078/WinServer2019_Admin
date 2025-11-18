using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinServer2019
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
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

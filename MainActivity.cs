using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.IO;

namespace WinServer2019
{
    public partial class MainActivity : Form
    {
        private string username;
        private string password;
        private string domain;
        private string scriptPath;
        private PowerShellExecutor psExecutor;
        private System.Threading.CancellationTokenSource cancellationTokenSource;
        private bool isExecuting = false;

        public MainActivity(string user, string pass, string dom)
        {
            InitializeComponent();
            username = user;
            password = pass;
            domain = dom;
            
            // Get the Scripts folder path
            scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            
            // Initialize PowerShell executor
            psExecutor = new PowerShellExecutor(username, password, domain, scriptPath);
        }

        private void MainActivity_Load(object sender, EventArgs e)
        {
            lblDomain.Text = $"Domain: {domain} | User: {username}";
            AppendOutput($"Connected to domain: {domain}");
            AppendOutput($"Logged in as: {username}");
            AppendOutput("System ready. Select an action from the tabs above.");
        }

        private void AppendOutput(string text, Color? color = null)
        {
            if (rtbOutput.InvokeRequired)
            {
                rtbOutput.Invoke(new Action(() => AppendOutput(text, color)));
                return;
            }

            rtbOutput.SelectionStart = rtbOutput.TextLength;
            rtbOutput.SelectionLength = 0;
            rtbOutput.SelectionColor = color ?? Color.Lime;
            rtbOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            rtbOutput.SelectionColor = rtbOutput.ForeColor;
            rtbOutput.ScrollToCaret();
        }

        private void UpdateStatus(string text)
        {
            if (statusLabel.Owner.InvokeRequired)
            {
                statusLabel.Owner.Invoke(new Action(() => UpdateStatus(text)));
                return;
            }
            statusLabel.Text = text;
        }

        // ===== PC MANAGEMENT EVENT HANDLERS =====
        private void BtnGetStatus_Click(object sender, EventArgs e)
        {
            string script = @"
                $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                Get-AllPCStatus -Targets $targets
            ";
            ExecutePowerShellCommand(script, "Getting status of all PCs (PC-1 to PC-35)...");
        }

        private void BtnShutdownSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Invoke-PCShutdown -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Shutting down PC-{pcNumber}...");
            }
        }

        private void BtnShutdownRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Invoke-PCShutdown -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Shutting down PCs {start} to {end}...");
            }
        }

        private void BtnShutdownAll_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Are you sure you want to shutdown ALL PCs (PC-1 to PC-35)?"))
            {
                string script = @"
                    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                    Invoke-PCShutdown -Targets $targets
                ";
                ExecutePowerShellCommand(script, "Shutting down all PCs...");
            }
        }

        private void BtnRestartSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Invoke-PCRestart -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Restarting PC-{pcNumber}...");
            }
        }

        private void BtnRestartRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Invoke-PCRestart -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Restarting PCs {start} to {end}...");
            }
        }

        private void BtnRestartAll_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Are you sure you want to restart ALL PCs (PC-1 to PC-35)?"))
            {
                string script = @"
                    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                    Invoke-PCRestart -Targets $targets
                ";
                ExecutePowerShellCommand(script, "Restarting all PCs...");
            }
        }

        private void BtnCustomCommand_Click(object sender, EventArgs e)
        {
            // Show options like Main.ps1 does
            using (var optionForm = new Form())
            {
                optionForm.Text = "Custom Command Options";
                optionForm.Size = new System.Drawing.Size(400, 250);
                optionForm.StartPosition = FormStartPosition.CenterParent;
                optionForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                optionForm.MaximizeBox = false;
                optionForm.MinimizeBox = false;

                Label lblPrompt = new Label
                {
                    Text = "Execute Custom PowerShell Command\n\nSelect target:",
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(350, 60),
                    Font = new System.Drawing.Font("Segoe UI", 10)
                };

                RadioButton rbSingle = new RadioButton
                {
                    Text = "Execute on specific PC",
                    Location = new System.Drawing.Point(30, 90),
                    Size = new System.Drawing.Size(300, 25),
                    Checked = true
                };

                RadioButton rbRange = new RadioButton
                {
                    Text = "Execute on range of PCs",
                    Location = new System.Drawing.Point(30, 120),
                    Size = new System.Drawing.Size(300, 25)
                };

                RadioButton rbAll = new RadioButton
                {
                    Text = "Execute on ALL PCs (PC-1 to PC-35)",
                    Location = new System.Drawing.Point(30, 150),
                    Size = new System.Drawing.Size(300, 25)
                };

                Button btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(150, 185),
                    Size = new System.Drawing.Size(80, 30)
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(240, 185),
                    Size = new System.Drawing.Size(80, 30)
                };

                optionForm.Controls.AddRange(new Control[] { lblPrompt, rbSingle, rbRange, rbAll, btnOK, btnCancel });
                optionForm.AcceptButton = btnOK;
                optionForm.CancelButton = btnCancel;

                if (optionForm.ShowDialog() == DialogResult.OK)
                {
                    string command = PromptForInput("Enter PowerShell command to execute:", multiline: true);
                    if (string.IsNullOrEmpty(command))
                    {
                        MessageBox.Show("No command provided.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string script = "";
                    
                    if (rbSingle.Checked)
                    {
                        string pcNumber = PromptForInput("Enter PC number (1-35):");
                        if (!string.IsNullOrEmpty(pcNumber))
                        {
                            script = $@"
                                $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                                Invoke-CustomPSCommand -Targets $targets -Command '{command.Replace("'", "''")}'
                            ";
                            ExecutePowerShellCommand(script, $"Executing custom command on PC-{pcNumber}...");
                        }
                    }
                    else if (rbRange.Checked)
                    {
                        string start = PromptForInput("Enter start PC number:");
                        string end = PromptForInput("Enter end PC number:");
                        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
                        {
                            script = $@"
                                $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                                Invoke-CustomPSCommand -Targets $targets -Command '{command.Replace("'", "''")}'
                            ";
                            ExecutePowerShellCommand(script, $"Executing custom command on PCs {start} to {end}...");
                        }
                    }
                    else if (rbAll.Checked)
                    {
                        if (ConfirmAction("Are you sure you want to execute this command on ALL PCs (PC-1 to PC-35)?"))
                        {
                            script = $@"
                                $targets = 1..35 | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                                Invoke-CustomPSCommand -Targets $targets -Command '{command.Replace("'", "''")}'
                            ";
                            ExecutePowerShellCommand(script, "Executing custom command on all PCs...");
                        }
                    }
                }
            }
        }

        // ===== WEB BLOCKING EVENT HANDLERS =====
        private void BtnBlockSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Invoke-WebBlocking -Targets $targets -BlockedSites $script:blockedSites
                ";
                ExecutePowerShellCommand(script, $"Blocking web access on PC-{pcNumber}...");
            }
        }

        private void BtnBlockRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Invoke-WebBlocking -Targets $targets -BlockedSites $script:blockedSites
                ";
                ExecutePowerShellCommand(script, $"Blocking web access on PCs {start} to {end}...");
            }
        }

        private void BtnBlockAll_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Are you sure you want to block web access on ALL PCs (PC-1 to PC-35)?"))
            {
                string script = @"
                    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                    Invoke-WebBlocking -Targets $targets -BlockedSites $script:blockedSites
                ";
                ExecutePowerShellCommand(script, "Blocking web access on all PCs...");
            }
        }

        private void BtnUnblockSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Invoke-WebUnblocking -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Unblocking web access on PC-{pcNumber}...");
            }
        }

        private void BtnUnblockRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Invoke-WebUnblocking -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Unblocking web access on PCs {start} to {end}...");
            }
        }

        private void BtnUnblockAll_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Are you sure you want to unblock web access on ALL PCs (PC-1 to PC-35)?"))
            {
                string script = @"
                    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                    Invoke-WebUnblocking -Targets $targets
                ";
                ExecutePowerShellCommand(script, "Unblocking web access on all PCs...");
            }
        }

        private void BtnDeepScan_Click(object sender, EventArgs e)
        {
            string script = @"
                Invoke-DeepScan
            ";
            ExecutePowerShellCommand(script, "Performing deep scan of blocking status...");
        }

        private void BtnViewBlockLists_Click(object sender, EventArgs e)
        {
            string script = @"
                Show-BlockLists -BlockedSites $script:blockedSites -BlockListsFolder $script:blockListsFolder
            ";
            ExecutePowerShellCommand(script, "Loading block lists...");
        }

        private void BtnBlockAISites_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Block AI sites only on ALL PCs?"))
            {
                string script = @"
                    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                    Invoke-AIBlocking -Targets $targets -AISites $script:aiSitesOnly
                ";
                ExecutePowerShellCommand(script, "Blocking AI sites on all PCs...");
            }
        }

        // ===== UTILITIES EVENT HANDLERS =====
        private void BtnSyncTime_Click(object sender, EventArgs e)
        {
            string script = @"
                Sync-TimeToAllPCs
            ";
            ExecutePowerShellCommand(script, "Syncing time/date to all PCs...");
        }

        private void BtnCleanBackup_Click(object sender, EventArgs e)
        {
            string script = @"
                Invoke-BackupCleanup
            ";
            ExecutePowerShellCommand(script, "Cleaning backup hosts files...");
        }

        private void BtnViewHosts_Click(object sender, EventArgs e)
        {
            string script = @"
                Show-AllHostsFiles
            ";
            ExecutePowerShellCommand(script, "Retrieving hosts files from all PCs...");
        }

        private void BtnExportDBSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Export-MySQLDatabases -Targets $targets -ExportType 'SINGLE' -ScriptPath $script:scriptPath
                ";
                ExecutePowerShellCommand(script, $"Exporting MySQL database from PC-{pcNumber}...");
            }
        }

        private void BtnExportDBRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Export-MySQLDatabases -Targets $targets -ExportType 'RANGE' -ScriptPath $script:scriptPath
                ";
                ExecutePowerShellCommand(script, $"Exporting MySQL databases from PCs {start} to {end}...");
            }
        }

        private void BtnExportDBAll_Click(object sender, EventArgs e)
        {
            string script = @"
                $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                Export-MySQLDatabases -Targets $targets -ExportType 'ALL' -ScriptPath $script:scriptPath
            ";
            ExecutePowerShellCommand(script, "Exporting MySQL databases from all PCs...");
        }

        private void BtnCheckEnvVars_Click(object sender, EventArgs e)
        {
            string script = @"
                Test-AndroidJavaEnvironment
            ";
            ExecutePowerShellCommand(script, "Checking environment variables...");
        }

        private void BtnCleanTempSingle_Click(object sender, EventArgs e)
        {
            string pcNumber = PromptForInput("Enter PC number (1-35):");
            if (!string.IsNullOrEmpty(pcNumber))
            {
                string script = $@"
                    $targets = @(""PC-{pcNumber}.$script:targetDomain"")
                    Clear-TempFiles -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Cleaning temporary files on PC-{pcNumber}...");
            }
        }

        private void BtnCleanTempRange_Click(object sender, EventArgs e)
        {
            string start = PromptForInput("Enter start PC number:");
            string end = PromptForInput("Enter end PC number:");
            if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                string script = $@"
                    $targets = {start}..{end} | ForEach-Object {{ ""PC-$_.$script:targetDomain"" }}
                    Clear-TempFiles -Targets $targets
                ";
                ExecutePowerShellCommand(script, $"Cleaning temporary files on PCs {start} to {end}...");
            }
        }

        private void BtnCleanTempAll_Click(object sender, EventArgs e)
        {
            string script = @"
                $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
                Clear-TempFiles -Targets $targets
            ";
            ExecutePowerShellCommand(script, "Cleaning temporary files on all PCs...");
        }

        // ===== HELPER METHODS =====
        private async void ExecutePowerShellCommand(string command, string statusMessage)
        {
            // Clear output at the start
            if (rtbOutput.InvokeRequired)
            {
                rtbOutput.Invoke(new Action(() => rtbOutput.Clear()));
            }
            else
            {
                rtbOutput.Clear();
            }

            // Create new cancellation token
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new System.Threading.CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            SetExecutionState(true);

            try
            {
                UpdateStatus(statusMessage);
                AppendOutput(statusMessage, Color.Yellow);
                AppendOutput("");

                var result = await Task.Run(() => 
                {
                    if (token.IsCancellationRequested)
                    {
                        return new PowerShellExecutionResult { Success = false };
                    }
                    
                    return psExecutor.ExecuteCommand(command, (output) => {
                        if (!token.IsCancellationRequested)
                        {
                            AppendOutput(output, Color.Lime);
                        }
                    }, token);
                }, token);
                
                // Display any errors that weren't already shown
                if (result.Errors.Any())
                {
                    AppendOutput("Errors encountered:", Color.Red);
                    foreach (var error in result.Errors)
                    {
                        AppendOutput($"  ERROR: {error}", Color.Red);
                    }
                }

                AppendOutput("");
                
                if (token.IsCancellationRequested)
                {
                    AppendOutput("EXECUTION STOPPED BY USER", Color.Red);
                    UpdateStatus("Execution stopped");
                }
                else
                {
                    UpdateStatus("Execution completed");
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                AppendOutput("EXECUTION CANCELLED", Color.Red);
                UpdateStatus("Execution cancelled");
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    AppendOutput($"Error: {ex.Message}", Color.Red);
                    UpdateStatus("Execution failed");
                }
            }
            finally
            {
                SetExecutionState(false);
            }
        }

        private string PromptForInput(string message, bool multiline = false)
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "Input Required";
                inputForm.Size = new Size(400, multiline ? 250 : 150);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label { Text = message, Location = new Point(10, 10), Size = new Size(360, 20) };
                
                Control inputControl;
                if (multiline)
                {
                    inputControl = new TextBox 
                    { 
                        Location = new Point(10, 35), 
                        Size = new Size(360, 120),
                        Multiline = true,
                        ScrollBars = ScrollBars.Vertical
                    };
                }
                else
                {
                    inputControl = new TextBox { Location = new Point(10, 35), Size = new Size(360, 25) };
                }
                
                var btnOk = new Button 
                { 
                    Text = "OK", 
                    DialogResult = DialogResult.OK, 
                    Location = new Point(200, multiline ? 165 : 70), 
                    Size = new Size(80, 30) 
                };
                
                var btnCancel = new Button 
                { 
                    Text = "Cancel", 
                    DialogResult = DialogResult.Cancel, 
                    Location = new Point(290, multiline ? 165 : 70), 
                    Size = new Size(80, 30) 
                };

                inputForm.Controls.AddRange(new Control[] { label, inputControl, btnOk, btnCancel });
                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                return inputForm.ShowDialog() == DialogResult.OK ? inputControl.Text : string.Empty;
            }
        }

        private bool ConfirmAction(string message)
        {
            return MessageBox.Show(message, "Confirm Action", MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void BtnClearOutput_Click(object sender, EventArgs e)
        {
            if (rtbOutput.InvokeRequired)
            {
                rtbOutput.Invoke(new Action(() => BtnClearOutput_Click(sender, e)));
                return;
            }
            rtbOutput.Clear();
            rtbOutput.SelectionColor = Color.Lime;
            rtbOutput.AppendText("Output console...\n");
            UpdateStatus("Output cleared");
        }

        private void BtnStopExecution_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                AppendOutput("STOP REQUESTED - Cancelling execution...", Color.Red);
                UpdateStatus("Stopping execution...");
                
                // Re-enable all buttons immediately after cancellation
                Task.Delay(500).ContinueWith(_ => 
                {
                    SetExecutionState(false);
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void SetExecutionState(bool executing)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetExecutionState(executing)));
                return;
            }
            
            isExecuting = executing;
            btnStopExecution.Enabled = executing;
            
            // Disable all action buttons during execution
            foreach (Control tab in tabControl.TabPages)
            {
                foreach (Control ctrl in tab.Controls)
                {
                    if (ctrl is Button)
                    {
                        ctrl.Enabled = !executing;
                    }
                }
            }
        }

        private void BtnMonitoring_Click(object sender, EventArgs e)
        {
            try
            {
                var monitoringForm = new MonitoringForm();
                monitoringForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening monitoring: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainActivity_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ConfirmAction("Are you sure you want to exit?"))
            {
                psExecutor?.Dispose();
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}

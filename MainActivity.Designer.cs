namespace WinServer2019
{
    partial class MainActivity : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPCManagement;
        private System.Windows.Forms.TabPage tabWebBlocking;
        private System.Windows.Forms.TabPage tabUtilities;
        private System.Windows.Forms.Label lblDomain;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.RichTextBox rtbOutput;
        
        // PC Management buttons
        private System.Windows.Forms.Button btnGetStatus;
        private System.Windows.Forms.Button btnShutdownSingle;
        private System.Windows.Forms.Button btnShutdownRange;
        private System.Windows.Forms.Button btnShutdownAll;
        private System.Windows.Forms.Button btnRestartSingle;
        private System.Windows.Forms.Button btnRestartRange;
        private System.Windows.Forms.Button btnRestartAll;
        private System.Windows.Forms.Button btnCustomCommand;
        
        // Web Blocking buttons
        private System.Windows.Forms.Button btnBlockSingle;
        private System.Windows.Forms.Button btnBlockRange;
        private System.Windows.Forms.Button btnBlockAll;
        private System.Windows.Forms.Button btnUnblockSingle;
        private System.Windows.Forms.Button btnUnblockRange;
        private System.Windows.Forms.Button btnUnblockAll;
        private System.Windows.Forms.Button btnDeepScan;
        private System.Windows.Forms.Button btnViewBlockLists;
        private System.Windows.Forms.Button btnBlockAISites;
        
        // Utilities buttons
        private System.Windows.Forms.Button btnSyncTime;
        private System.Windows.Forms.Button btnCleanBackup;
        private System.Windows.Forms.Button btnViewHosts;
        private System.Windows.Forms.Button btnExportDBSingle;
        private System.Windows.Forms.Button btnExportDBRange;
        private System.Windows.Forms.Button btnExportDBAll;
        private System.Windows.Forms.Button btnCheckEnvVars;
        private System.Windows.Forms.Button btnCleanTempSingle;
        private System.Windows.Forms.Button btnCleanTempRange;
        private System.Windows.Forms.Button btnCleanTempAll;
        
        // Control buttons
        private System.Windows.Forms.Button btnClearOutput;
        private System.Windows.Forms.Button btnStopExecution;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPCManagement = new System.Windows.Forms.TabPage();
            this.btnGetStatus = new System.Windows.Forms.Button();
            this.btnShutdownSingle = new System.Windows.Forms.Button();
            this.btnShutdownRange = new System.Windows.Forms.Button();
            this.btnShutdownAll = new System.Windows.Forms.Button();
            this.btnRestartSingle = new System.Windows.Forms.Button();
            this.btnRestartRange = new System.Windows.Forms.Button();
            this.btnRestartAll = new System.Windows.Forms.Button();
            this.btnCustomCommand = new System.Windows.Forms.Button();
            this.tabWebBlocking = new System.Windows.Forms.TabPage();
            this.btnBlockSingle = new System.Windows.Forms.Button();
            this.btnBlockRange = new System.Windows.Forms.Button();
            this.btnBlockAll = new System.Windows.Forms.Button();
            this.btnUnblockSingle = new System.Windows.Forms.Button();
            this.btnUnblockRange = new System.Windows.Forms.Button();
            this.btnUnblockAll = new System.Windows.Forms.Button();
            this.btnDeepScan = new System.Windows.Forms.Button();
            this.btnViewBlockLists = new System.Windows.Forms.Button();
            this.btnBlockAISites = new System.Windows.Forms.Button();
            this.tabUtilities = new System.Windows.Forms.TabPage();
            this.btnSyncTime = new System.Windows.Forms.Button();
            this.btnCleanBackup = new System.Windows.Forms.Button();
            this.btnViewHosts = new System.Windows.Forms.Button();
            this.btnExportDBSingle = new System.Windows.Forms.Button();
            this.btnExportDBRange = new System.Windows.Forms.Button();
            this.btnExportDBAll = new System.Windows.Forms.Button();
            this.btnCheckEnvVars = new System.Windows.Forms.Button();
            this.btnCleanTempSingle = new System.Windows.Forms.Button();
            this.btnCleanTempRange = new System.Windows.Forms.Button();
            this.btnCleanTempAll = new System.Windows.Forms.Button();
            this.btnClearOutput = new System.Windows.Forms.Button();
            this.btnStopExecution = new System.Windows.Forms.Button();
            this.lblDomain = new System.Windows.Forms.Label();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.tabControl.SuspendLayout();
            this.tabPCManagement.SuspendLayout();
            this.tabWebBlocking.SuspendLayout();
            this.tabUtilities.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPCManagement);
            this.tabControl.Controls.Add(this.tabWebBlocking);
            this.tabControl.Controls.Add(this.tabUtilities);
            this.tabControl.Location = new System.Drawing.Point(12, 80);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(360, 400);
            this.tabControl.TabIndex = 0;
            // 
            // tabPCManagement
            // 
            this.tabPCManagement.BackColor = System.Drawing.Color.White;
            this.tabPCManagement.Controls.Add(this.btnGetStatus);
            this.tabPCManagement.Controls.Add(this.btnShutdownSingle);
            this.tabPCManagement.Controls.Add(this.btnShutdownRange);
            this.tabPCManagement.Controls.Add(this.btnShutdownAll);
            this.tabPCManagement.Controls.Add(this.btnRestartSingle);
            this.tabPCManagement.Controls.Add(this.btnRestartRange);
            this.tabPCManagement.Controls.Add(this.btnRestartAll);
            this.tabPCManagement.Controls.Add(this.btnCustomCommand);
            this.tabPCManagement.Location = new System.Drawing.Point(4, 22);
            this.tabPCManagement.Name = "tabPCManagement";
            this.tabPCManagement.Padding = new System.Windows.Forms.Padding(3);
            this.tabPCManagement.Size = new System.Drawing.Size(352, 374);
            this.tabPCManagement.TabIndex = 0;
            this.tabPCManagement.Text = "PC Management";
            // 
            // btnGetStatus
            // 
            this.btnGetStatus.Location = new System.Drawing.Point(10, 10);
            this.btnGetStatus.Name = "btnGetStatus";
            this.btnGetStatus.Size = new System.Drawing.Size(330, 35);
            this.btnGetStatus.TabIndex = 0;
            this.btnGetStatus.Text = "Get Status of ALL PCs (PC-1 to PC-35)";
            this.btnGetStatus.Click += new System.EventHandler(this.BtnGetStatus_Click);
            // 
            // btnShutdownSingle
            // 
            this.btnShutdownSingle.Location = new System.Drawing.Point(10, 50);
            this.btnShutdownSingle.Name = "btnShutdownSingle";
            this.btnShutdownSingle.Size = new System.Drawing.Size(330, 35);
            this.btnShutdownSingle.TabIndex = 1;
            this.btnShutdownSingle.Text = "Shutdown Single PC";
            this.btnShutdownSingle.Click += new System.EventHandler(this.BtnShutdownSingle_Click);
            // 
            // btnShutdownRange
            // 
            this.btnShutdownRange.Location = new System.Drawing.Point(10, 90);
            this.btnShutdownRange.Name = "btnShutdownRange";
            this.btnShutdownRange.Size = new System.Drawing.Size(330, 35);
            this.btnShutdownRange.TabIndex = 2;
            this.btnShutdownRange.Text = "Shutdown Range of PCs";
            this.btnShutdownRange.Click += new System.EventHandler(this.BtnShutdownRange_Click);
            // 
            // btnShutdownAll
            // 
            this.btnShutdownAll.Location = new System.Drawing.Point(10, 130);
            this.btnShutdownAll.Name = "btnShutdownAll";
            this.btnShutdownAll.Size = new System.Drawing.Size(330, 35);
            this.btnShutdownAll.TabIndex = 3;
            this.btnShutdownAll.Text = "Shutdown ALL PCs (PC-1 to PC-35)";
            this.btnShutdownAll.Click += new System.EventHandler(this.BtnShutdownAll_Click);
            // 
            // btnRestartSingle
            // 
            this.btnRestartSingle.Location = new System.Drawing.Point(10, 170);
            this.btnRestartSingle.Name = "btnRestartSingle";
            this.btnRestartSingle.Size = new System.Drawing.Size(330, 35);
            this.btnRestartSingle.TabIndex = 4;
            this.btnRestartSingle.Text = "Restart Single PC";
            this.btnRestartSingle.Click += new System.EventHandler(this.BtnRestartSingle_Click);
            // 
            // btnRestartRange
            // 
            this.btnRestartRange.Location = new System.Drawing.Point(10, 210);
            this.btnRestartRange.Name = "btnRestartRange";
            this.btnRestartRange.Size = new System.Drawing.Size(330, 35);
            this.btnRestartRange.TabIndex = 5;
            this.btnRestartRange.Text = "Restart Range of PCs";
            this.btnRestartRange.Click += new System.EventHandler(this.BtnRestartRange_Click);
            // 
            // btnRestartAll
            // 
            this.btnRestartAll.Location = new System.Drawing.Point(10, 250);
            this.btnRestartAll.Name = "btnRestartAll";
            this.btnRestartAll.Size = new System.Drawing.Size(330, 35);
            this.btnRestartAll.TabIndex = 6;
            this.btnRestartAll.Text = "Restart ALL PCs (PC-1 to PC-35)";
            this.btnRestartAll.Click += new System.EventHandler(this.BtnRestartAll_Click);
            // 
            // btnCustomCommand
            // 
            this.btnCustomCommand.Location = new System.Drawing.Point(10, 290);
            this.btnCustomCommand.Name = "btnCustomCommand";
            this.btnCustomCommand.Size = new System.Drawing.Size(330, 35);
            this.btnCustomCommand.TabIndex = 7;
            this.btnCustomCommand.Text = "Execute Custom PowerShell Command";
            this.btnCustomCommand.Click += new System.EventHandler(this.BtnCustomCommand_Click);
            // 
            // tabWebBlocking
            // 
            this.tabWebBlocking.BackColor = System.Drawing.Color.White;
            this.tabWebBlocking.Controls.Add(this.btnBlockSingle);
            this.tabWebBlocking.Controls.Add(this.btnBlockRange);
            this.tabWebBlocking.Controls.Add(this.btnBlockAll);
            this.tabWebBlocking.Controls.Add(this.btnUnblockSingle);
            this.tabWebBlocking.Controls.Add(this.btnUnblockRange);
            this.tabWebBlocking.Controls.Add(this.btnUnblockAll);
            this.tabWebBlocking.Controls.Add(this.btnDeepScan);
            this.tabWebBlocking.Controls.Add(this.btnViewBlockLists);
            this.tabWebBlocking.Controls.Add(this.btnBlockAISites);
            this.tabWebBlocking.Location = new System.Drawing.Point(4, 22);
            this.tabWebBlocking.Name = "tabWebBlocking";
            this.tabWebBlocking.Padding = new System.Windows.Forms.Padding(3);
            this.tabWebBlocking.Size = new System.Drawing.Size(352, 374);
            this.tabWebBlocking.TabIndex = 1;
            this.tabWebBlocking.Text = "Web Blocking";
            // 
            // btnBlockSingle
            // 
            this.btnBlockSingle.Location = new System.Drawing.Point(10, 10);
            this.btnBlockSingle.Name = "btnBlockSingle";
            this.btnBlockSingle.Size = new System.Drawing.Size(330, 35);
            this.btnBlockSingle.TabIndex = 0;
            this.btnBlockSingle.Text = "Block Web/DNS - Single PC";
            this.btnBlockSingle.Click += new System.EventHandler(this.BtnBlockSingle_Click);
            // 
            // btnBlockRange
            // 
            this.btnBlockRange.Location = new System.Drawing.Point(10, 50);
            this.btnBlockRange.Name = "btnBlockRange";
            this.btnBlockRange.Size = new System.Drawing.Size(330, 35);
            this.btnBlockRange.TabIndex = 1;
            this.btnBlockRange.Text = "Block Web/DNS - Range of PCs";
            this.btnBlockRange.Click += new System.EventHandler(this.BtnBlockRange_Click);
            // 
            // btnBlockAll
            // 
            this.btnBlockAll.Location = new System.Drawing.Point(10, 90);
            this.btnBlockAll.Name = "btnBlockAll";
            this.btnBlockAll.Size = new System.Drawing.Size(330, 35);
            this.btnBlockAll.TabIndex = 2;
            this.btnBlockAll.Text = "Block Web/DNS - ALL PCs";
            this.btnBlockAll.Click += new System.EventHandler(this.BtnBlockAll_Click);
            // 
            // btnUnblockSingle
            // 
            this.btnUnblockSingle.Location = new System.Drawing.Point(10, 130);
            this.btnUnblockSingle.Name = "btnUnblockSingle";
            this.btnUnblockSingle.Size = new System.Drawing.Size(330, 35);
            this.btnUnblockSingle.TabIndex = 3;
            this.btnUnblockSingle.Text = "Unblock Web/DNS - Single PC";
            this.btnUnblockSingle.Click += new System.EventHandler(this.BtnUnblockSingle_Click);
            // 
            // btnUnblockRange
            // 
            this.btnUnblockRange.Location = new System.Drawing.Point(10, 170);
            this.btnUnblockRange.Name = "btnUnblockRange";
            this.btnUnblockRange.Size = new System.Drawing.Size(330, 35);
            this.btnUnblockRange.TabIndex = 4;
            this.btnUnblockRange.Text = "Unblock Web/DNS - Range of PCs";
            this.btnUnblockRange.Click += new System.EventHandler(this.BtnUnblockRange_Click);
            // 
            // btnUnblockAll
            // 
            this.btnUnblockAll.Location = new System.Drawing.Point(10, 210);
            this.btnUnblockAll.Name = "btnUnblockAll";
            this.btnUnblockAll.Size = new System.Drawing.Size(330, 35);
            this.btnUnblockAll.TabIndex = 5;
            this.btnUnblockAll.Text = "Unblock Web/DNS - ALL PCs";
            this.btnUnblockAll.Click += new System.EventHandler(this.BtnUnblockAll_Click);
            // 
            // btnDeepScan
            // 
            this.btnDeepScan.Location = new System.Drawing.Point(10, 250);
            this.btnDeepScan.Name = "btnDeepScan";
            this.btnDeepScan.Size = new System.Drawing.Size(330, 35);
            this.btnDeepScan.TabIndex = 6;
            this.btnDeepScan.Text = "Deep Scan - Check Blocking Status";
            this.btnDeepScan.Click += new System.EventHandler(this.BtnDeepScan_Click);
            // 
            // btnViewBlockLists
            // 
            this.btnViewBlockLists.Location = new System.Drawing.Point(10, 290);
            this.btnViewBlockLists.Name = "btnViewBlockLists";
            this.btnViewBlockLists.Size = new System.Drawing.Size(330, 35);
            this.btnViewBlockLists.TabIndex = 7;
            this.btnViewBlockLists.Text = "View Current Block Lists";
            this.btnViewBlockLists.Click += new System.EventHandler(this.BtnViewBlockLists_Click);
            // 
            // btnBlockAISites
            // 
            this.btnBlockAISites.Location = new System.Drawing.Point(10, 330);
            this.btnBlockAISites.Name = "btnBlockAISites";
            this.btnBlockAISites.Size = new System.Drawing.Size(330, 35);
            this.btnBlockAISites.TabIndex = 8;
            this.btnBlockAISites.Text = "Block AI Sites ONLY - ALL PCs";
            this.btnBlockAISites.Click += new System.EventHandler(this.BtnBlockAISites_Click);
            // 
            // tabUtilities
            // 
            this.tabUtilities.BackColor = System.Drawing.Color.White;
            this.tabUtilities.Controls.Add(this.btnSyncTime);
            this.tabUtilities.Controls.Add(this.btnCleanBackup);
            this.tabUtilities.Controls.Add(this.btnViewHosts);
            this.tabUtilities.Controls.Add(this.btnExportDBSingle);
            this.tabUtilities.Controls.Add(this.btnExportDBRange);
            this.tabUtilities.Controls.Add(this.btnExportDBAll);
            this.tabUtilities.Controls.Add(this.btnCheckEnvVars);
            this.tabUtilities.Controls.Add(this.btnCleanTempSingle);
            this.tabUtilities.Controls.Add(this.btnCleanTempRange);
            this.tabUtilities.Controls.Add(this.btnCleanTempAll);
            this.tabUtilities.Location = new System.Drawing.Point(4, 22);
            this.tabUtilities.Name = "tabUtilities";
            this.tabUtilities.Size = new System.Drawing.Size(352, 374);
            this.tabUtilities.TabIndex = 2;
            this.tabUtilities.Text = "Utilities";
            // 
            // btnSyncTime
            // 
            this.btnSyncTime.Location = new System.Drawing.Point(10, 10);
            this.btnSyncTime.Name = "btnSyncTime";
            this.btnSyncTime.Size = new System.Drawing.Size(330, 30);
            this.btnSyncTime.TabIndex = 0;
            this.btnSyncTime.Text = "Sync Time to ALL PCs";
            this.btnSyncTime.Click += new System.EventHandler(this.BtnSyncTime_Click);
            // 
            // btnCleanBackup
            // 
            this.btnCleanBackup.Location = new System.Drawing.Point(10, 45);
            this.btnCleanBackup.Name = "btnCleanBackup";
            this.btnCleanBackup.Size = new System.Drawing.Size(330, 30);
            this.btnCleanBackup.TabIndex = 1;
            this.btnCleanBackup.Text = "Clean Backup Hosts Files";
            this.btnCleanBackup.Click += new System.EventHandler(this.BtnCleanBackup_Click);
            // 
            // btnViewHosts
            // 
            this.btnViewHosts.Location = new System.Drawing.Point(10, 80);
            this.btnViewHosts.Name = "btnViewHosts";
            this.btnViewHosts.Size = new System.Drawing.Size(330, 30);
            this.btnViewHosts.TabIndex = 2;
            this.btnViewHosts.Text = "View All PC Hosts Files";
            this.btnViewHosts.Click += new System.EventHandler(this.BtnViewHosts_Click);
            // 
            // btnExportDBSingle
            // 
            this.btnExportDBSingle.Location = new System.Drawing.Point(10, 115);
            this.btnExportDBSingle.Name = "btnExportDBSingle";
            this.btnExportDBSingle.Size = new System.Drawing.Size(330, 30);
            this.btnExportDBSingle.TabIndex = 3;
            this.btnExportDBSingle.Text = "Export MySQL DB - Single PC";
            this.btnExportDBSingle.Click += new System.EventHandler(this.BtnExportDBSingle_Click);
            // 
            // btnExportDBRange
            // 
            this.btnExportDBRange.Location = new System.Drawing.Point(10, 150);
            this.btnExportDBRange.Name = "btnExportDBRange";
            this.btnExportDBRange.Size = new System.Drawing.Size(330, 30);
            this.btnExportDBRange.TabIndex = 4;
            this.btnExportDBRange.Text = "Export MySQL DB - Range of PCs";
            this.btnExportDBRange.Click += new System.EventHandler(this.BtnExportDBRange_Click);
            // 
            // btnExportDBAll
            // 
            this.btnExportDBAll.Location = new System.Drawing.Point(10, 185);
            this.btnExportDBAll.Name = "btnExportDBAll";
            this.btnExportDBAll.Size = new System.Drawing.Size(330, 30);
            this.btnExportDBAll.TabIndex = 5;
            this.btnExportDBAll.Text = "Export MySQL DB - ALL PCs";
            this.btnExportDBAll.Click += new System.EventHandler(this.BtnExportDBAll_Click);
            // 
            // btnCheckEnvVars
            // 
            this.btnCheckEnvVars.Location = new System.Drawing.Point(10, 220);
            this.btnCheckEnvVars.Name = "btnCheckEnvVars";
            this.btnCheckEnvVars.Size = new System.Drawing.Size(330, 30);
            this.btnCheckEnvVars.TabIndex = 6;
            this.btnCheckEnvVars.Text = "Check/Fix Environment Variables";
            this.btnCheckEnvVars.Click += new System.EventHandler(this.BtnCheckEnvVars_Click);
            // 
            // btnCleanTempSingle
            // 
            this.btnCleanTempSingle.Location = new System.Drawing.Point(10, 255);
            this.btnCleanTempSingle.Name = "btnCleanTempSingle";
            this.btnCleanTempSingle.Size = new System.Drawing.Size(330, 30);
            this.btnCleanTempSingle.TabIndex = 7;
            this.btnCleanTempSingle.Text = "Clean Temp Files - Single PC";
            this.btnCleanTempSingle.Click += new System.EventHandler(this.BtnCleanTempSingle_Click);
            // 
            // btnCleanTempRange
            // 
            this.btnCleanTempRange.Location = new System.Drawing.Point(10, 290);
            this.btnCleanTempRange.Name = "btnCleanTempRange";
            this.btnCleanTempRange.Size = new System.Drawing.Size(330, 30);
            this.btnCleanTempRange.TabIndex = 8;
            this.btnCleanTempRange.Text = "Clean Temp Files - Range of PCs";
            this.btnCleanTempRange.Click += new System.EventHandler(this.BtnCleanTempRange_Click);
            // 
            // btnCleanTempAll
            // 
            this.btnCleanTempAll.Location = new System.Drawing.Point(10, 325);
            this.btnCleanTempAll.Name = "btnCleanTempAll";
            this.btnCleanTempAll.Size = new System.Drawing.Size(330, 30);
            this.btnCleanTempAll.TabIndex = 9;
            this.btnCleanTempAll.Text = "Clean Temp Files - ALL PCs";
            this.btnCleanTempAll.Click += new System.EventHandler(this.BtnCleanTempAll_Click);
            // 
            // btnClearOutput
            // 
            this.btnClearOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnClearOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearOutput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnClearOutput.ForeColor = System.Drawing.Color.White;
            this.btnClearOutput.Location = new System.Drawing.Point(1252, 79);
            this.btnClearOutput.Name = "btnClearOutput";
            this.btnClearOutput.Size = new System.Drawing.Size(90, 30);
            this.btnClearOutput.TabIndex = 5;
            this.btnClearOutput.Text = "Clear";
            this.btnClearOutput.UseVisualStyleBackColor = false;
            this.btnClearOutput.Click += new System.EventHandler(this.BtnClearOutput_Click);
            // 
            // btnStopExecution
            // 
            this.btnStopExecution.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnStopExecution.Enabled = false;
            this.btnStopExecution.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopExecution.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStopExecution.ForeColor = System.Drawing.Color.White;
            this.btnStopExecution.Location = new System.Drawing.Point(1347, 79);
            this.btnStopExecution.Name = "btnStopExecution";
            this.btnStopExecution.Size = new System.Drawing.Size(90, 30);
            this.btnStopExecution.TabIndex = 6;
            this.btnStopExecution.Text = "Stop";
            this.btnStopExecution.UseVisualStyleBackColor = false;
            this.btnStopExecution.Click += new System.EventHandler(this.BtnStopExecution_Click);
            // 
            // lblDomain
            // 
            this.lblDomain.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDomain.Location = new System.Drawing.Point(12, 50);
            this.lblDomain.Name = "lblDomain";
            this.lblDomain.Size = new System.Drawing.Size(1425, 20);
            this.lblDomain.TabIndex = 3;
            this.lblDomain.Text = "Domain: csitlab.local";
            this.lblDomain.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWelcome
            // 
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblWelcome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.lblWelcome.Location = new System.Drawing.Point(12, 15);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(1425, 30);
            this.lblWelcome.TabIndex = 4;
            this.lblWelcome.Text = "PC MANAGEMENT SYSTEM";
            this.lblWelcome.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 493);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1449, 22);
            this.statusStrip.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";
            // 
            // rtbOutput
            // 
            this.rtbOutput.BackColor = System.Drawing.Color.Black;
            this.rtbOutput.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbOutput.ForeColor = System.Drawing.Color.Lime;
            this.rtbOutput.Location = new System.Drawing.Point(380, 115);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.ReadOnly = true;
            this.rtbOutput.Size = new System.Drawing.Size(1057, 365);
            this.rtbOutput.TabIndex = 1;
            this.rtbOutput.Text = "Output console...\n";
            // 
            // MainActivity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1449, 515);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.btnStopExecution);
            this.Controls.Add(this.btnClearOutput);
            this.Controls.Add(this.rtbOutput);
            this.Controls.Add(this.lblDomain);
            this.Controls.Add(this.lblWelcome);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainActivity";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PC Management System - Main";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainActivity_FormClosing);
            this.Load += new System.EventHandler(this.MainActivity_Load);
            this.tabControl.ResumeLayout(false);
            this.tabPCManagement.ResumeLayout(false);
            this.tabWebBlocking.ResumeLayout(false);
            this.tabUtilities.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}


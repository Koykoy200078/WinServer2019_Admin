namespace WinServer2019
{
    partial class MonitoringForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Label lblClients;
        private System.Windows.Forms.ListView lvClients;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.RichTextBox rtbActivityLog;
        private System.Windows.Forms.Label lblDetails;
        private System.Windows.Forms.Panel panelDetails;
        private System.Windows.Forms.RichTextBox rtbClientDetails;

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
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.lblClients = new System.Windows.Forms.Label();
            this.lvClients = new System.Windows.Forms.ListView();
            this.lblLog = new System.Windows.Forms.Label();
            this.rtbActivityLog = new System.Windows.Forms.RichTextBox();
            this.lblDetails = new System.Windows.Forms.Label();
            this.panelDetails = new System.Windows.Forms.Panel();
            this.rtbClientDetails = new System.Windows.Forms.RichTextBox();
            this.panelDetails.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(20, 20);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(300, 25);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Server Status: Stopped";
            // 
            // btnStartStop
            // 
            this.btnStartStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnStartStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartStop.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnStartStop.ForeColor = System.Drawing.Color.White;
            this.btnStartStop.Location = new System.Drawing.Point(350, 15);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(180, 35);
            this.btnStartStop.TabIndex = 1;
            this.btnStartStop.Text = "Start Monitoring Server";
            this.btnStartStop.UseVisualStyleBackColor = false;
            this.btnStartStop.Click += new System.EventHandler(this.BtnStartStop_Click);
            // 
            // lblClients
            // 
            this.lblClients.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblClients.Location = new System.Drawing.Point(20, 60);
            this.lblClients.Name = "lblClients";
            this.lblClients.Size = new System.Drawing.Size(200, 25);
            this.lblClients.TabIndex = 2;
            this.lblClients.Text = "Connected Clients:";
            // 
            // lvClients
            // 
            this.lvClients.Font = new System.Drawing.Font("Consolas", 9F);
            this.lvClients.FullRowSelect = true;
            this.lvClients.GridLines = true;
            this.lvClients.HideSelection = false;
            this.lvClients.Location = new System.Drawing.Point(20, 90);
            this.lvClients.Name = "lvClients";
            this.lvClients.Size = new System.Drawing.Size(1526, 705);
            this.lvClients.TabIndex = 3;
            this.lvClients.UseCompatibleStateImageBehavior = false;
            this.lvClients.View = System.Windows.Forms.View.Details;
            this.lvClients.SelectedIndexChanged += new System.EventHandler(this.LvClients_SelectedIndexChanged);
            this.lvClients.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.LvClients_MouseDoubleClick);
            // 
            // lblLog
            // 
            this.lblLog.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblLog.Location = new System.Drawing.Point(20, 798);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(150, 21);
            this.lblLog.TabIndex = 4;
            this.lblLog.Text = "Activity Log:";
            this.lblLog.Click += new System.EventHandler(this.lblLog_Click);
            // 
            // rtbActivityLog
            // 
            this.rtbActivityLog.BackColor = System.Drawing.Color.Black;
            this.rtbActivityLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbActivityLog.ForeColor = System.Drawing.Color.Lime;
            this.rtbActivityLog.Location = new System.Drawing.Point(20, 822);
            this.rtbActivityLog.Name = "rtbActivityLog";
            this.rtbActivityLog.ReadOnly = true;
            this.rtbActivityLog.Size = new System.Drawing.Size(1526, 180);
            this.rtbActivityLog.TabIndex = 5;
            this.rtbActivityLog.Text = "";
            // 
            // lblDetails
            // 
            this.lblDetails.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblDetails.Location = new System.Drawing.Point(1552, 60);
            this.lblDetails.Name = "lblDetails";
            this.lblDetails.Size = new System.Drawing.Size(110, 25);
            this.lblDetails.TabIndex = 6;
            this.lblDetails.Text = "Client Details:";
            // 
            // panelDetails
            // 
            this.panelDetails.BackColor = System.Drawing.Color.White;
            this.panelDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelDetails.Controls.Add(this.rtbClientDetails);
            this.panelDetails.Location = new System.Drawing.Point(1552, 90);
            this.panelDetails.Name = "panelDetails";
            this.panelDetails.Size = new System.Drawing.Size(340, 912);
            this.panelDetails.TabIndex = 7;
            // 
            // rtbClientDetails
            // 
            this.rtbClientDetails.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.rtbClientDetails.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbClientDetails.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rtbClientDetails.Location = new System.Drawing.Point(-1, -3);
            this.rtbClientDetails.Name = "rtbClientDetails";
            this.rtbClientDetails.ReadOnly = true;
            this.rtbClientDetails.Size = new System.Drawing.Size(336, 914);
            this.rtbClientDetails.TabIndex = 0;
            this.rtbClientDetails.Text = "";
            // 
            // MonitoringForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1004);
            this.Controls.Add(this.panelDetails);
            this.Controls.Add(this.lblDetails);
            this.Controls.Add(this.rtbActivityLog);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.lvClients);
            this.Controls.Add(this.lblClients);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.lblStatus);
            this.Name = "MonitoringForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Real-Time PC Monitoring";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MonitoringForm_FormClosing);
            this.panelDetails.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

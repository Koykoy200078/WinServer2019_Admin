namespace WinServer2019
{
    partial class ScreenViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel controlPanel;
        private System.Windows.Forms.Label lblPCName;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label lblQuality;
        private System.Windows.Forms.ComboBox cmbQuality;
        private System.Windows.Forms.Button btnToggleFit;
        private System.Windows.Forms.Button btnToggleFullScreen;
        private System.Windows.Forms.Button btnSaveSnapshot;
        private System.Windows.Forms.Button btnSendMessage;
        private System.Windows.Forms.Button btnFreeze;

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
            if (disposing && server != null)
            {
                server.OnClientUpdate -= Server_OnClientUpdate;
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
            this.controlPanel = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.cmbQuality = new System.Windows.Forms.ComboBox();
            this.lblQuality = new System.Windows.Forms.Label();
            this.btnToggleFit = new System.Windows.Forms.Button();
            this.btnToggleFullScreen = new System.Windows.Forms.Button();
            this.btnSaveSnapshot = new System.Windows.Forms.Button();
            this.btnSendMessage = new System.Windows.Forms.Button();
            this.btnFreeze = new System.Windows.Forms.Button();
            this.lblPCName = new System.Windows.Forms.Label();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.controlPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // controlPanel
            // 
            this.controlPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.controlPanel.Controls.Add(this.btnClose);
            this.controlPanel.Controls.Add(this.btnRefresh);
            this.controlPanel.Controls.Add(this.cmbQuality);
            this.controlPanel.Controls.Add(this.lblQuality);
            this.controlPanel.Controls.Add(this.btnToggleFit);
            this.controlPanel.Controls.Add(this.btnToggleFullScreen);
            this.controlPanel.Controls.Add(this.btnSaveSnapshot);
            this.controlPanel.Controls.Add(this.btnSendMessage);
            this.controlPanel.Controls.Add(this.btnFreeze);
            this.controlPanel.Controls.Add(this.lblPCName);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(0, 0);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(1024, 50);
            this.controlPanel.TabIndex = 0;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(935, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 30);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(845, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(80, 30);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // cmbQuality
            // 
            this.cmbQuality.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbQuality.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuality.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbQuality.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbQuality.ForeColor = System.Drawing.Color.White;
            this.cmbQuality.FormattingEnabled = true;
            this.cmbQuality.Items.AddRange(new object[] {
            "480p (Fast)",
            "720p (Balanced)",
            "1080p (High Quality)"});
            this.cmbQuality.Location = new System.Drawing.Point(705, 13);
            this.cmbQuality.Name = "cmbQuality";
            this.cmbQuality.Size = new System.Drawing.Size(130, 23);
            this.cmbQuality.TabIndex = 4;
            // 
            // lblQuality
            // 
            this.lblQuality.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblQuality.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblQuality.ForeColor = System.Drawing.Color.White;
            this.lblQuality.Location = new System.Drawing.Point(650, 16);
            this.lblQuality.Name = "lblQuality";
            this.lblQuality.Size = new System.Drawing.Size(50, 20);
            this.lblQuality.TabIndex = 5;
            this.lblQuality.Text = "Quality:";
            // 
            // btnToggleFit
            // 
            this.btnToggleFit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleFit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(55)))));
            this.btnToggleFit.FlatAppearance.BorderSize = 0;
            this.btnToggleFit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleFit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnToggleFit.ForeColor = System.Drawing.Color.White;
            this.btnToggleFit.Location = new System.Drawing.Point(555, 10);
            this.btnToggleFit.Name = "btnToggleFit";
            this.btnToggleFit.Size = new System.Drawing.Size(85, 30);
            this.btnToggleFit.TabIndex = 6;
            this.btnToggleFit.Text = "Mode: Fit";
            this.btnToggleFit.UseVisualStyleBackColor = false;
            // 
            // btnToggleFullScreen
            // 
            this.btnToggleFullScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleFullScreen.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(55)))));
            this.btnToggleFullScreen.FlatAppearance.BorderSize = 0;
            this.btnToggleFullScreen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleFullScreen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnToggleFullScreen.ForeColor = System.Drawing.Color.White;
            this.btnToggleFullScreen.Location = new System.Drawing.Point(440, 10);
            this.btnToggleFullScreen.Name = "btnToggleFullScreen";
            this.btnToggleFullScreen.Size = new System.Drawing.Size(105, 30);
            this.btnToggleFullScreen.TabIndex = 7;
            this.btnToggleFullScreen.Text = "⛶ Full Screen";
            this.btnToggleFullScreen.UseVisualStyleBackColor = false;
            // 
            // btnSaveSnapshot
            // 
            this.btnSaveSnapshot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveSnapshot.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.btnSaveSnapshot.FlatAppearance.BorderSize = 0;
            this.btnSaveSnapshot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSnapshot.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveSnapshot.ForeColor = System.Drawing.Color.White;
            this.btnSaveSnapshot.Location = new System.Drawing.Point(165, 10);
            this.btnSaveSnapshot.Name = "btnSaveSnapshot";
            this.btnSaveSnapshot.Size = new System.Drawing.Size(85, 30);
            this.btnSaveSnapshot.TabIndex = 8;
            this.btnSaveSnapshot.Text = "📸 Save";
            this.btnSaveSnapshot.UseVisualStyleBackColor = false;
            // 
            // btnSendMessage
            // 
            this.btnSendMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSendMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSendMessage.FlatAppearance.BorderSize = 0;
            this.btnSendMessage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSendMessage.ForeColor = System.Drawing.Color.White;
            this.btnSendMessage.Location = new System.Drawing.Point(255, 10);
            this.btnSendMessage.Name = "btnSendMessage";
            this.btnSendMessage.Size = new System.Drawing.Size(90, 30);
            this.btnSendMessage.TabIndex = 9;
            this.btnSendMessage.Text = "📨 Msg";
            this.btnSendMessage.UseVisualStyleBackColor = false;
            // 
            // btnFreeze
            // 
            this.btnFreeze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFreeze.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.btnFreeze.FlatAppearance.BorderSize = 0;
            this.btnFreeze.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFreeze.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFreeze.ForeColor = System.Drawing.Color.White;
            this.btnFreeze.Location = new System.Drawing.Point(350, 10);
            this.btnFreeze.Name = "btnFreeze";
            this.btnFreeze.Size = new System.Drawing.Size(85, 30);
            this.btnFreeze.TabIndex = 10;
            this.btnFreeze.Text = "⚠ Freeze";
            this.btnFreeze.UseVisualStyleBackColor = false;
            // 
            // lblPCName
            // 
            this.lblPCName.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPCName.ForeColor = System.Drawing.Color.White;
            this.lblPCName.Location = new System.Drawing.Point(15, 14);
            this.lblPCName.Name = "lblPCName";
            this.lblPCName.Size = new System.Drawing.Size(145, 22);
            this.lblPCName.TabIndex = 0;
            this.lblPCName.Text = "Viewing: PC-1";
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.statusPanel.Controls.Add(this.lblHint);
            this.statusPanel.Controls.Add(this.lblStatus);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(0, 740);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(1024, 28);
            this.statusPanel.TabIndex = 8;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.LightGreen;
            this.lblStatus.Location = new System.Drawing.Point(10, 5);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(700, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Waiting for screen data...";
            // 
            // lblHint
            // 
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblHint.ForeColor = System.Drawing.Color.DarkGray;
            this.lblHint.Location = new System.Drawing.Point(720, 5);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(294, 20);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "F11 / Double-click: Fullscreen | Esc: Exit";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.Black;
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 50);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(1024, 690);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 1;
            this.pictureBox.TabStop = false;
            // 
            // ScreenViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ClientSize = new System.Drawing.Size(1024, 768);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.controlPanel);
            this.Name = "ScreenViewerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Screen Viewer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScreenViewerForm_FormClosing);
            this.controlPanel.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    
    }
}
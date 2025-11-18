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
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.PictureBox pictureBox;

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
            this.controlPanel = new System.Windows.Forms.Panel();
            this.lblPCName = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.controlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // controlPanel
            // 
            this.controlPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.controlPanel.Controls.Add(this.btnClose);
            this.controlPanel.Controls.Add(this.btnRefresh);
            this.controlPanel.Controls.Add(this.lblStatus);
            this.controlPanel.Controls.Add(this.lblPCName);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(0, 0);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(1024, 50);
            this.controlPanel.TabIndex = 0;
            // 
            // lblPCName
            // 
            this.lblPCName.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPCName.ForeColor = System.Drawing.Color.White;
            this.lblPCName.Location = new System.Drawing.Point(10, 15);
            this.lblPCName.Name = "lblPCName";
            this.lblPCName.Size = new System.Drawing.Size(300, 20);
            this.lblPCName.TabIndex = 0;
            this.lblPCName.Text = "Viewing: PC-1";
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.LightGray;
            this.lblStatus.Location = new System.Drawing.Point(320, 15);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(400, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Waiting for screen data...";
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(730, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 30);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "? Refresh";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(840, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 30);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "? Close";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.Black;
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 50);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(1024, 718);
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
            this.Controls.Add(this.controlPanel);
            this.Name = "ScreenViewerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Screen Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScreenViewerForm_FormClosing);
            this.controlPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}

using System.Drawing;

namespace MultiCom.Server
{
    partial class ServerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (streamingTimer != null)
                {
                    streamingTimer.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.labelActions = new System.Windows.Forms.Label();
            this.comboBoxCameras = new System.Windows.Forms.ComboBox();
            this.labelCamera = new System.Windows.Forms.Label();
            this.btnRefreshCamera = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            // this.panelMetrics = new System.Windows.Forms.Panel();
            // this.lblErrors = new System.Windows.Forms.Label();
            // this.lblBitrate = new System.Windows.Forms.Label();
            // this.lblFrames = new System.Windows.Forms.Label();
            this.listClients = new System.Windows.Forms.ListBox();
            this.listEvents = new System.Windows.Forms.ListBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.streamingTimer = new System.Windows.Forms.Timer(this.components);
            this.panelSidebar.SuspendLayout();
            // this.panelMetrics.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.BackColor = Color.FromArgb(32, 34, 37);
            // this.panelSidebar.Controls.Add(this.panelMetrics);
            this.panelSidebar.Controls.Add(this.btnRefreshCamera);
            this.panelSidebar.Controls.Add(this.btnStop);
            this.panelSidebar.Controls.Add(this.btnStart);
            this.panelSidebar.Controls.Add(this.comboBoxCameras);
            this.panelSidebar.Controls.Add(this.labelCamera);
            this.panelSidebar.Controls.Add(this.labelActions);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Padding = new System.Windows.Forms.Padding(16);
            this.panelSidebar.Size = new System.Drawing.Size(280, 640);
            this.panelSidebar.TabIndex = 0;
            // 
            // labelActions
            // 
            this.labelActions.AutoSize = true;
            this.labelActions.ForeColor = Color.LightGray;
            this.labelActions.Location = new System.Drawing.Point(16, 16);
            this.labelActions.Name = "labelActions";
            this.labelActions.Size = new System.Drawing.Size(159, 20);
            this.labelActions.TabIndex = 0;
            this.labelActions.Text = "Presence coordination";
            // 
            // labelCamera
            // 
            this.labelCamera.AutoSize = true;
            this.labelCamera.ForeColor = Color.LightGray;
            this.labelCamera.Location = new System.Drawing.Point(16, 48);
            this.labelCamera.Name = "labelCamera";
            this.labelCamera.Size = new System.Drawing.Size(120, 20);
            this.labelCamera.TabIndex = 4;
            this.labelCamera.Text = "Seleccionar Cámara:";
            // 
            // comboBoxCameras
            // 
            this.comboBoxCameras.BackColor = Color.FromArgb(47, 49, 54);
            this.comboBoxCameras.ForeColor = Color.White;
            this.comboBoxCameras.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCameras.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxCameras.FormattingEnabled = true;
            this.comboBoxCameras.Location = new System.Drawing.Point(16, 72);
            this.comboBoxCameras.Name = "comboBoxCameras";
            this.comboBoxCameras.Size = new System.Drawing.Size(248, 28);
            this.comboBoxCameras.TabIndex = 5;
            // 
            // btnRefreshCamera
            // 
            this.btnRefreshCamera.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshCamera.ForeColor = Color.White;
            this.btnRefreshCamera.Location = new System.Drawing.Point(16, 224);
            this.btnRefreshCamera.Name = "btnRefreshCamera";
            this.btnRefreshCamera.Size = new System.Drawing.Size(248, 40);
            this.btnRefreshCamera.TabIndex = 3;
            this.btnRefreshCamera.Text = "Refresh Cameras";
            this.btnRefreshCamera.UseVisualStyleBackColor = true;
            this.btnRefreshCamera.Click += new System.EventHandler(this.OnRefreshClick);
            // 
            // btnStop
            // 
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.ForeColor = Color.White;
            this.btnStop.Location = new System.Drawing.Point(16, 168);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(248, 40);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop service";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.OnStopClick);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = Color.FromArgb(88, 101, 242);
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.ForeColor = Color.White;
            this.btnStart.Location = new System.Drawing.Point(16, 112);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(248, 40);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start service";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.OnStartClick);
            // 
            // panelMetrics
            // 
            /*
            this.panelMetrics.BackColor = Color.FromArgb(47, 49, 54);
            this.panelMetrics.Controls.Add(this.lblErrors);
            this.panelMetrics.Controls.Add(this.lblBitrate);
            this.panelMetrics.Controls.Add(this.lblFrames);
            this.panelMetrics.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelMetrics.Location = new System.Drawing.Point(16, 464);
            this.panelMetrics.Name = "panelMetrics";
            this.panelMetrics.Padding = new System.Windows.Forms.Padding(16);
            this.panelMetrics.Size = new System.Drawing.Size(248, 160);
            this.panelMetrics.TabIndex = 4;
            */
            /*
            // lblErrors - COMENTADO
            this.lblErrors.AutoSize = true;
            this.lblErrors.ForeColor = Color.LightGray;
            this.lblErrors.Location = new System.Drawing.Point(16, 96);
            this.lblErrors.Name = "lblErrors";
            this.lblErrors.Size = new System.Drawing.Size(127, 20);
            this.lblErrors.TabIndex = 2;
            this.lblErrors.Text = "Last push: -";
            // 
            // lblBitrate - COMENTADO
            this.lblBitrate.AutoSize = true;
            this.lblBitrate.ForeColor = Color.LightGray;
            this.lblBitrate.Location = new System.Drawing.Point(16, 64);
            this.lblBitrate.Name = "lblBitrate";
            this.lblBitrate.Size = new System.Drawing.Size(131, 20);
            this.lblBitrate.TabIndex = 1;
            this.lblBitrate.Text = "Snapshots: 0";
            // 
            // lblFrames - COMENTADO
            this.lblFrames.AutoSize = true;
            this.lblFrames.ForeColor = Color.LightGray;
            this.lblFrames.Location = new System.Drawing.Point(16, 32);
            this.lblFrames.Name = "lblFrames";
            this.lblFrames.Size = new System.Drawing.Size(95, 20);
            this.lblFrames.TabIndex = 0;
            this.lblFrames.Text = "Online: 0";
            */
            // 
            // listClients
            // 
            this.listClients.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listClients.BackColor = Color.FromArgb(47, 49, 54);
            this.listClients.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listClients.ForeColor = Color.White;
            this.listClients.FormattingEnabled = true;
            this.listClients.ItemHeight = 20;
            this.listClients.Location = new System.Drawing.Point(304, 72);
            this.listClients.Name = "listClients";
            this.listClients.Size = new System.Drawing.Size(664, 320);
            this.listClients.TabIndex = 1;
            // 
            // listEvents
            // 
            this.listEvents.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listEvents.BackColor = Color.FromArgb(47, 49, 54);
            this.listEvents.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listEvents.ForeColor = Color.White;
            this.listEvents.FormattingEnabled = true;
            this.listEvents.ItemHeight = 20;
            this.listEvents.Location = new System.Drawing.Point(304, 416);
            this.listEvents.Name = "listEvents";
            this.listEvents.Size = new System.Drawing.Size(664, 200);
            this.listEvents.TabIndex = 2;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = Color.White;
            this.labelTitle.Location = new System.Drawing.Point(296, 16);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(303, 37);
            this.labelTitle.TabIndex = 3;
            this.labelTitle.Text = "MultiCom Presence Hub";
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = Color.FromArgb(47, 49, 54);
            this.pictureBox.Location = new System.Drawing.Point(304, 80);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(640, 480);
            this.pictureBox.TabIndex = 4;
            this.pictureBox.TabStop = false;
            // 
            // streamingTimer
            // 
            this.streamingTimer.Interval = 1000;
            this.streamingTimer.Tick += new System.EventHandler(this.OnMetricsTick);
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(54, 57, 63);
            this.ClientSize = new System.Drawing.Size(1000, 640);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.listEvents);
            this.Controls.Add(this.listClients);
            this.Controls.Add(this.panelSidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "ServerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MultiCom Server";
            this.Load += new System.EventHandler(this.OnFormLoaded);
            this.panelSidebar.ResumeLayout(false);
            this.panelSidebar.PerformLayout();
            /*
            this.panelMetrics.ResumeLayout(false);
            this.panelMetrics.PerformLayout();
            */
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Label labelActions;
        private System.Windows.Forms.Label labelCamera;
        private System.Windows.Forms.ComboBox comboBoxCameras;
        private System.Windows.Forms.Button btnRefreshCamera;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        
        // private System.Windows.Forms.Panel panelMetrics;
        // private System.Windows.Forms.Label lblErrors;
        // private System.Windows.Forms.Label lblBitrate;
        // private System.Windows.Forms.Label lblFrames;
        private System.Windows.Forms.ListBox listClients;
        private System.Windows.Forms.ListBox listEvents;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Timer streamingTimer;
    }
}


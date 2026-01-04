using System.Drawing;

namespace MultiCom.Client
{
    partial class ClientForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (uiTimer != null)
                {
                    uiTimer.Dispose();
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
            this.listMembers = new System.Windows.Forms.ListBox();
            this.labelMembers = new System.Windows.Forms.Label();
            this.panelStats = new System.Windows.Forms.Panel();
            this.lblLoss = new System.Windows.Forms.Label();
            this.lblJitter = new System.Windows.Forms.Label();
            this.lblLatency = new System.Windows.Forms.Label();
            this.lblFps = new System.Windows.Forms.Label();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelChat = new System.Windows.Forms.Panel();
            this.listChat = new System.Windows.Forms.ListBox();
            this.listDiagnostics = new System.Windows.Forms.ListBox();
            this.panelInput = new System.Windows.Forms.Panel();
            this.btnSendMessage = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.flowVideo = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBoxVideo = new System.Windows.Forms.PictureBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.uiTimer = new System.Windows.Forms.Timer(this.components);
            this.panelSidebar.SuspendLayout();
            this.panelStats.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.panelChat.SuspendLayout();
            this.panelInput.SuspendLayout();
            this.flowVideo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
            this.flowVideo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(34)))), ((int)(((byte)(37)))));
            this.panelSidebar.Controls.Add(this.panelChat);
            this.panelSidebar.Controls.Add(this.listMembers);
            this.panelSidebar.Controls.Add(this.labelMembers);
            this.panelSidebar.Controls.Add(this.panelStats);
            this.panelSidebar.Controls.Add(this.btnDisconnect);
            this.panelSidebar.Controls.Add(this.btnConnect);
            this.panelSidebar.Controls.Add(this.btnSettings);
            this.panelSidebar.Controls.Add(this.lblProfileName);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Padding = new System.Windows.Forms.Padding(16);
            this.panelSidebar.Size = new System.Drawing.Size(280, 720);
            this.panelSidebar.TabIndex = 0;
            // 
            // listMembers
            // 
            this.listMembers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listMembers.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.listMembers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listMembers.ForeColor = System.Drawing.Color.White;
            this.listMembers.FormattingEnabled = true;
            this.listMembers.ItemHeight = 20;
            this.listMembers.Location = new System.Drawing.Point(16, 272);
            this.listMembers.Name = "listMembers";
            this.listMembers.Size = new System.Drawing.Size(248, 220);
            this.listMembers.TabIndex = 5;
            // 
            // labelMembers
            // 
            this.labelMembers.AutoSize = true;
            this.labelMembers.ForeColor = System.Drawing.Color.LightGray;
            this.labelMembers.Location = new System.Drawing.Point(16, 240);
            this.labelMembers.Name = "labelMembers";
            this.labelMembers.Size = new System.Drawing.Size(116, 20);
            this.labelMembers.TabIndex = 9;
            this.labelMembers.Text = "Members online";
            // 
            // panelStats
            // 
            this.panelStats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.panelStats.Controls.Add(this.lblLoss);
            this.panelStats.Controls.Add(this.lblJitter);
            this.panelStats.Controls.Add(this.lblLatency);
            this.panelStats.Controls.Add(this.lblFps);
            this.panelStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStats.Location = new System.Drawing.Point(16, 608);
            this.panelStats.Name = "panelStats";
            this.panelStats.Padding = new System.Windows.Forms.Padding(12);
            this.panelStats.Size = new System.Drawing.Size(248, 96);
            this.panelStats.TabIndex = 8;
            // 
            // lblLoss
            // 
            this.lblLoss.AutoSize = true;
            this.lblLoss.ForeColor = System.Drawing.Color.LightGray;
            this.lblLoss.Location = new System.Drawing.Point(12, 68);
            this.lblLoss.Name = "lblLoss";
            this.lblLoss.Size = new System.Drawing.Size(83, 20);
            this.lblLoss.TabIndex = 3;
            this.lblLoss.Text = "Loss: 0 pkts";
            // 
            // lblJitter
            // 
            this.lblJitter.AutoSize = true;
            this.lblJitter.ForeColor = System.Drawing.Color.LightGray;
            this.lblJitter.Location = new System.Drawing.Point(12, 48);
            this.lblJitter.Name = "lblJitter";
            this.lblJitter.Size = new System.Drawing.Size(79, 20);
            this.lblJitter.TabIndex = 2;
            this.lblJitter.Text = "Jitter: 0 ms";
            // 
            // lblLatency
            // 
            this.lblLatency.AutoSize = true;
            this.lblLatency.ForeColor = System.Drawing.Color.LightGray;
            this.lblLatency.Location = new System.Drawing.Point(12, 28);
            this.lblLatency.Name = "lblLatency";
            this.lblLatency.Size = new System.Drawing.Size(97, 20);
            this.lblLatency.TabIndex = 1;
            this.lblLatency.Text = "Latency: 0 ms";
            // 
            // lblFps
            // 
            this.lblFps.AutoSize = true;
            this.lblFps.ForeColor = System.Drawing.Color.LightGray;
            this.lblFps.Location = new System.Drawing.Point(12, 8);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(71, 20);
            this.lblFps.TabIndex = 0;
            this.lblFps.Text = "FPS: 0 fps";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDisconnect.ForeColor = System.Drawing.Color.White;
            this.btnDisconnect.Location = new System.Drawing.Point(16, 190);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(248, 36);
            this.btnDisconnect.TabIndex = 4;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.OnDisconnect);
            // 
            // btnConnect
            // 
            this.btnConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(181)))), ((int)(((byte)(129)))));
            this.btnConnect.FlatAppearance.BorderSize = 0;
            this.btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnect.ForeColor = System.Drawing.Color.White;
            this.btnConnect.Location = new System.Drawing.Point(16, 146);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(248, 36);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = false;
            this.btnConnect.Click += new System.EventHandler(this.OnConnect);
            // 
            // btnToggleCamera
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(101)))), ((int)(((byte)(242)))));
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(16, 48);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(248, 36);
            this.btnSettings.TabIndex = 1;
            this.btnSettings.Text = "Open settings";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.OnOpenSettings);
            // 
            // lblProfileName
            // 
            this.lblProfileName.AutoSize = true;
            this.lblProfileName.ForeColor = System.Drawing.Color.LightGray;
            this.lblProfileName.Location = new System.Drawing.Point(16, 16);
            this.lblProfileName.Name = "lblProfileName";
            this.lblProfileName.Size = new System.Drawing.Size(133, 20);
            this.lblProfileName.TabIndex = 0;
            this.lblProfileName.Text = "Signed in as User";
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(57)))), ((int)(((byte)(63)))));
            this.panelMain.Controls.Add(this.flowVideo);
            this.panelMain.Controls.Add(this.labelTitle);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(280, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(16);
            this.panelMain.Size = new System.Drawing.Size(980, 720);
            this.panelMain.TabIndex = 1;
            // 
            // panelChat
            // 
            this.panelChat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.panelChat.Controls.Add(this.listChat);
            this.panelChat.Controls.Add(this.panelInput);
            this.panelChat.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelChat.Location = new System.Drawing.Point(16, 420);
            this.panelChat.Name = "panelChat";
            this.panelChat.Padding = new System.Windows.Forms.Padding(8);
            this.panelChat.Size = new System.Drawing.Size(248, 284);
            this.panelChat.TabIndex = 10;
            // 
            // listChat
            // 
            this.listChat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.listChat.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listChat.ForeColor = System.Drawing.Color.White;
            this.listChat.FormattingEnabled = true;
            this.listChat.ItemHeight = 20;
            this.listChat.Location = new System.Drawing.Point(8, 8);
            this.listChat.Name = "listChat";
            this.listChat.Size = new System.Drawing.Size(232, 220);
            this.listChat.TabIndex = 1;
            // 
            // panelInput
            // 
            this.panelInput.Controls.Add(this.btnSendMessage);
            this.panelInput.Controls.Add(this.txtMessage);
            this.panelInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelInput.Location = new System.Drawing.Point(16, 384);
            this.panelInput.Name = "panelInput";
            this.panelInput.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.panelInput.Size = new System.Drawing.Size(916, 48);
            this.panelInput.TabIndex = 2;
            // 
            // btnSendMessage
            // 
            this.btnSendMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(101)))), ((int)(((byte)(242)))));
            this.btnSendMessage.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSendMessage.FlatAppearance.BorderSize = 0;
            this.btnSendMessage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendMessage.ForeColor = System.Drawing.Color.White;
            this.btnSendMessage.Location = new System.Drawing.Point(776, 8);
            this.btnSendMessage.Name = "btnSendMessage";
            this.btnSendMessage.Size = new System.Drawing.Size(140, 40);
            this.btnSendMessage.TabIndex = 1;
            this.btnSendMessage.Text = "Send";
            this.btnSendMessage.UseVisualStyleBackColor = false;
            this.btnSendMessage.Click += new System.EventHandler(this.OnSendMessage);
            // 
            // txtMessage
            // 
            this.txtMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(43)))), ((int)(((byte)(47)))));
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMessage.ForeColor = System.Drawing.Color.White;
            this.txtMessage.Location = new System.Drawing.Point(0, 8);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(916, 40);
            this.txtMessage.TabIndex = 0;
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnMessageKeyDown);
            // 
            // flowVideo
            // 
            this.flowVideo.AutoScroll = true;
            this.flowVideo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.flowVideo.Controls.Add(this.pictureBoxVideo);
            this.flowVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowVideo.Location = new System.Drawing.Point(16, 16);
            this.flowVideo.Name = "flowVideo";
            this.flowVideo.Padding = new System.Windows.Forms.Padding(8);
            this.flowVideo.Size = new System.Drawing.Size(948, 688);
            this.flowVideo.TabIndex = 1;
            // 
            // pictureBoxVideo
            // 
            this.pictureBoxVideo.BackColor = System.Drawing.Color.Black;
            this.pictureBoxVideo.Location = new System.Drawing.Point(10, 10);
            this.pictureBoxVideo.Margin = new System.Windows.Forms.Padding(8);
            this.pictureBoxVideo.Name = "pictureBoxVideo";
            this.pictureBoxVideo.Size = new System.Drawing.Size(640, 480);
            this.pictureBoxVideo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxVideo.TabIndex = 0;
            this.pictureBoxVideo.TabStop = false;
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(8, 16);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(309, 37);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "MultiCom Client Space";
            // 
            // uiTimer
            // 
            this.uiTimer.Interval = 1000;
            this.uiTimer.Tick += new System.EventHandler(this.OnUiTimerTick);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(57)))), ((int)(((byte)(63)))));
            this.ClientSize = new System.Drawing.Size(1260, 720);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelSidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "ClientForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MultiCom Client";
            this.Load += new System.EventHandler(this.OnClientLoaded);
            this.panelSidebar.ResumeLayout(false);
            this.panelSidebar.PerformLayout();
            this.panelStats.ResumeLayout(false);
            this.panelStats.PerformLayout();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.panelChat.ResumeLayout(false);
            this.panelInput.ResumeLayout(false);
            this.panelInput.PerformLayout();
            this.flowVideo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.ListBox listMembers;
        private System.Windows.Forms.Label labelMembers;
        private System.Windows.Forms.Panel panelStats;
        private System.Windows.Forms.Label lblLoss;
        private System.Windows.Forms.Label lblJitter;
        private System.Windows.Forms.Label lblLatency;
        private System.Windows.Forms.Label lblFps;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Label lblProfileName;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.FlowLayoutPanel flowVideo;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Panel panelChat;
        private System.Windows.Forms.Panel panelInput;
        private System.Windows.Forms.Button btnSendMessage;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.ListBox listChat;
        private System.Windows.Forms.ListBox listDiagnostics;
        private System.Windows.Forms.Timer uiTimer;
        private System.Windows.Forms.PictureBox pictureBoxVideo;
    }
}

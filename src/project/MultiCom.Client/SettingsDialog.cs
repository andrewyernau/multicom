using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MultiCom.Client.Audio;

namespace MultiCom.Client
{
    internal sealed class SettingsDialog : Form
    {
        private readonly IList<string> cameraNames;
        private readonly IReadOnlyList<AudioDeviceInfo> audioDevices;
        private readonly IReadOnlyList<IPAddress> interfaceOptions;

        private TextBox txtDisplayName;
        private ComboBox comboCameras;
        private ComboBox comboAudio;
        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private NumericUpDown numFps;
        private NumericUpDown numQuality;
        private ComboBox comboInterfaces;
        private TextBox txtServerIp;

        public ClientPreferences Preferences { get; private set; }

        public SettingsDialog(ClientPreferences current, IList<string> cameraNames, IReadOnlyList<AudioDeviceInfo> audioDevices, IReadOnlyList<IPAddress> interfaceOptions)
        {
            this.cameraNames = cameraNames ?? Array.Empty<string>();
            this.audioDevices = audioDevices ?? Array.Empty<AudioDeviceInfo>();
            this.interfaceOptions = interfaceOptions ?? Array.Empty<IPAddress>();

            InitializeComponent();
            BindData(current ?? new ClientPreferences());
        }

        private void InitializeComponent()
        {
            Text = "Client settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(47, 49, 54);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(420, 520);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 10,
                AutoSize = true,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            txtDisplayName = CreateTextBox();
            comboCameras = CreateComboBox();
            comboAudio = CreateComboBox();
            comboInterfaces = CreateComboBox();
            txtServerIp = CreateTextBox();

            numWidth = CreateNumeric(160, 1920, 640);
            numHeight = CreateNumeric(120, 1080, 360);
            numFps = CreateNumeric(5, 60, 20);
            numQuality = CreateNumeric(10, 100, 80);

            AddRow(layout, "Display name", txtDisplayName, 0);
            AddRow(layout, "Camera", comboCameras, 1);
            AddRow(layout, "Audio input", comboAudio, 2);
            AddRow(layout, "Frame width", numWidth, 3);
            AddRow(layout, "Frame height", numHeight, 4);
            AddRow(layout, "FPS", numFps, 5);
            AddRow(layout, "JPEG quality", numQuality, 6);
            AddRow(layout, "Local interface", comboInterfaces, 7);
            AddRow(layout, "Server IP", txtServerIp, 8);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
            };

            var btnSave = CreateButton("Save", Color.FromArgb(67, 181, 129));
            btnSave.Click += OnSave;
            var btnCancel = CreateButton("Cancel", Color.FromArgb(114, 118, 125));
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            layout.Controls.Add(buttonPanel, 0, 9);
            layout.SetColumnSpan(buttonPanel, 2);

            Controls.Add(layout);
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                BackColor = Color.FromArgb(32, 34, 37),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
            };
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                BackColor = Color.FromArgb(32, 34, 37),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
            };
        }

        private static NumericUpDown CreateNumeric(int min, int max, int value)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                BackColor = Color.FromArgb(32, 34, 37),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
            };
        }

        private static Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Margin = new Padding(8, 16, 0, 0)
            };
        }

        private void AddRow(TableLayoutPanel layout, string label, Control control, int rowIndex)
        {
            if (layout.RowStyles.Count <= rowIndex)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var lbl = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LightGray,
                Margin = new Padding(0, 8, 8, 0)
            };

            control.Margin = new Padding(0, 8, 0, 0);
            layout.Controls.Add(lbl, 0, rowIndex);
            layout.Controls.Add(control, 1, rowIndex);
        }

        private void BindData(ClientPreferences current)
        {
            txtDisplayName.Text = string.IsNullOrWhiteSpace(current.DisplayName) ? "Agent" : current.DisplayName;

            comboCameras.Items.Clear();
            if (cameraNames.Count == 0)
            {
                comboCameras.Items.Add("No cameras detected");
                comboCameras.SelectedIndex = 0;
                comboCameras.Enabled = false;
            }
            else
            {
                foreach (var name in cameraNames)
                {
                    comboCameras.Items.Add(name);
                }

                if (current.CameraIndex >= 0 && current.CameraIndex < cameraNames.Count)
                {
                    comboCameras.SelectedIndex = current.CameraIndex;
                }
                else
                {
                    comboCameras.SelectedIndex = 0;
                }
            }

            comboAudio.Items.Clear();
            if (audioDevices.Count == 0)
            {
                comboAudio.Items.Add("System default");
                comboAudio.SelectedIndex = 0;
                comboAudio.Enabled = false;
            }
            else
            {
                comboAudio.DisplayMember = nameof(AudioDeviceInfo.Name);
                foreach (var device in audioDevices)
                {
                    comboAudio.Items.Add(device);
                }

                var match = audioDevices.FirstOrDefault(d => string.Equals(d.Id, current.AudioDeviceId, StringComparison.OrdinalIgnoreCase));
                comboAudio.SelectedItem = match ?? audioDevices.First();
            }

            numWidth.Value = Clamp(current.CaptureWidth, (int)numWidth.Minimum, (int)numWidth.Maximum, (int)numWidth.Value);
            numHeight.Value = Clamp(current.CaptureHeight, (int)numHeight.Minimum, (int)numHeight.Maximum, (int)numHeight.Value);
            numFps.Value = Clamp(current.CaptureFps, (int)numFps.Minimum, (int)numFps.Maximum, (int)numFps.Value);
            numQuality.Value = Clamp((int)current.JpegQuality, (int)numQuality.Minimum, (int)numQuality.Maximum, (int)numQuality.Value);

            comboInterfaces.Items.Clear();
            comboInterfaces.Items.Add(new InterfaceOption("Automatic", null));
            foreach (var ip in interfaceOptions)
            {
                comboInterfaces.Items.Add(new InterfaceOption(ip.ToString(), ip));
            }

            var selectedInterface = comboInterfaces.Items
                .OfType<InterfaceOption>()
                .FirstOrDefault(o => current.PreferredInterface != null && Equals(o.Address, current.PreferredInterface));
            comboInterfaces.SelectedItem = selectedInterface ?? comboInterfaces.Items[0];

            txtServerIp.Text = current.ServerAddress != null ? current.ServerAddress.ToString() : string.Empty;
        }

        private static decimal Clamp(int value, int min, int max, int fallback)
        {
            if (value < min || value > max)
            {
                return fallback;
            }

            return value;
        }

        private void OnSave(object sender, EventArgs e)
        {
            var selectedDevice = comboAudio.SelectedItem as AudioDeviceInfo;
            var selectedInterface = comboInterfaces.SelectedItem as InterfaceOption;

            var prefs = new ClientPreferences
            {
                DisplayName = txtDisplayName.Text.Trim(),
                CameraIndex = comboCameras.Enabled ? comboCameras.SelectedIndex : -1,
                AudioDeviceId = selectedDevice != null ? selectedDevice.Id : null,
                CaptureWidth = (int)numWidth.Value,
                CaptureHeight = (int)numHeight.Value,
                CaptureFps = (int)numFps.Value,
                JpegQuality = (long)numQuality.Value,
                PreferredInterface = selectedInterface != null ? selectedInterface.Address : null,
                ServerAddress = ParseAddress(txtServerIp.Text),
            };

            Preferences = prefs;
            DialogResult = DialogResult.OK;
        }

        private static IPAddress ParseAddress(string text)
        {
            IPAddress address;
            return IPAddress.TryParse(text, out address) ? address : null;
        }

        private sealed class InterfaceOption
        {
            public InterfaceOption(string label, IPAddress address)
            {
                Label = label;
                Address = address;
            }

            public string Label { get; private set; }
            public IPAddress Address { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}

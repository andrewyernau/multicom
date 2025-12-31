using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiCom.Client.Audio;
using MultiCom.Shared.Chat;
using MultiCom.Shared.Networking;
using Touchless.Vision.Camera;

namespace MultiCom.Client
{
    public partial class ClientForm : Form
    {
        private const int PAYLOAD_BYTES = 12000;
        private const int TARGET_WIDTH = 640;
        private const int TARGET_HEIGHT = 360;
        private const int TARGET_FPS = 20;
        private const float SPEAKING_THRESHOLD = 0.18f;

        private readonly IPEndPoint videoEndpoint = MulticastChannels.BuildVideoEndpoint();
        private readonly IPEndPoint chatEndpoint = MulticastChannels.BuildChatEndpoint();
        private readonly IPEndPoint controlEndpoint = MulticastChannels.BuildControlEndpoint();
        private static readonly ImageCodecInfo JpegCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        private readonly List<Camera> availableCameras = new List<Camera>();
        private IReadOnlyList<AudioDeviceInfo> cachedAudioDevices = Array.Empty<AudioDeviceInfo>();
        private int selectedCameraIndex = -1;
        private string selectedAudioDeviceId;
        private IPAddress preferredInterfaceAddress;
        private IPAddress serverUnicastAddress;
        private string displayName = "Agent";
        private int captureWidth = TARGET_WIDTH;
        private int captureHeight = TARGET_HEIGHT;
        private int captureFps = TARGET_FPS;
        private long jpegQuality = 80L;

        private sealed class VideoTile
        {
            public VideoTile(string displayName)
            {
                Container = new Panel
                {
                    BackColor = Color.FromArgb(47, 49, 54),
                    Margin = new Padding(8),
                    Padding = new Padding(8),
                    Size = new Size(220, 150)
                };

                Surface = new PictureBox
                {
                    BackColor = Color.Black,
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                };

                Caption = new Label
                {
                    Dock = DockStyle.Bottom,
                    Height = 24,
                    ForeColor = Color.LightGray,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = displayName
                };

                Container.Controls.Add(Surface);
                Container.Controls.Add(Caption);
            }

            public Panel Container { get; private set; }
            public PictureBox Surface { get; private set; }
            public Label Caption { get; private set; }
        }

        private readonly Guid clientId = Guid.NewGuid();
        private readonly VideoFrameAssembler frameAssembler = new VideoFrameAssembler();
        private readonly PerformanceTracker performanceTracker = new PerformanceTracker();
        private readonly Dictionary<Guid, PresenceRecord> presenceRecords = new Dictionary<Guid, PresenceRecord>();
        private readonly Dictionary<Guid, VideoTile> videoTiles = new Dictionary<Guid, VideoTile>();
        private readonly object presenceGate = new object();
        private readonly TimeSpan presenceMinInterval = TimeSpan.FromMilliseconds(400);
        private readonly TimeSpan serverTimeoutWindow = TimeSpan.FromSeconds(6);
        private readonly TimeSpan serverGraceWindow = TimeSpan.FromSeconds(5);

        private CancellationTokenSource videoCts;
        private CancellationTokenSource chatCts;
        private CancellationTokenSource controlCts;
        private CancellationTokenSource cameraCts;
        private UdpClient chatSender;
        private UdpClient controlSender;
        private UdpClient videoSender;
        private CameraFrameSource frameSource;
        private System.Threading.Timer heartbeatTimer;
        private AudioLevelMonitor audioMonitor;
        private int frameNumber;
        private bool isSpeakingLocal;
        private bool serverReachable;
        private bool serverDisconnectNotified;
        private bool suppressServerAlertReset;
        private bool isDisconnecting;
        private DateTime lastPresenceSentUtc = DateTime.MinValue;
        private DateTime lastServerSnapshotUtc = DateTime.MinValue;
        private DateTime connectionStartedUtc = DateTime.MinValue;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void OnClientLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            UpdateProfileLabel();
            uiTimer.Start();
            LoadCameras();
            LoadAudioDevices();
            EnsureAudioMonitor();
            performanceTracker.Reset();
            StartNetworking();
        }

        private void ApplyDiscordPalette()
        {
            btnSendMessage.BackColor = Color.FromArgb(88, 101, 242);
            btnSendMessage.ForeColor = Color.White;
            btnToggleCamera.BackColor = Color.FromArgb(67, 181, 129);
            btnToggleCamera.ForeColor = Color.White;
            btnConnect.BackColor = Color.FromArgb(67, 181, 129);
            btnConnect.ForeColor = Color.White;
            btnSettings.BackColor = Color.FromArgb(88, 101, 242);
            btnSettings.ForeColor = Color.White;
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.ForeColor = Color.White;
            btnDisconnect.Enabled = false;
            btnConnect.Enabled = true;
        }

        private void StartNetworking()
        {
            if (videoCts != null || chatCts != null || controlCts != null)
            {
                Log("[WARN] Already connected.");
                return;
            }

            performanceTracker.Reset();

            try
            {
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                lock (presenceGate)
                {
                    presenceRecords.Clear();
                }

                ClearVideoTiles();
                UpdateMembersList();

                serverReachable = false;
                serverDisconnectNotified = false;
                suppressServerAlertReset = false;
                connectionStartedUtc = DateTime.UtcNow;
                lastServerSnapshotUtc = DateTime.MinValue;
                lastPresenceSentUtc = DateTime.MinValue;

                videoCts = new CancellationTokenSource();
                chatCts = new CancellationTokenSource();
                controlCts = new CancellationTokenSource();

                chatSender = CreateMulticastSender(true, chatEndpoint.Address);
                controlSender = CreateMulticastSender(true, controlEndpoint.Address);

                Task.Run(() => ListenVideoLoop(videoCts.Token));
                Task.Run(() => ListenChatLoop(chatCts.Token));
                Task.Run(() => ListenControlLoop(controlCts.Token));

                StartHeartbeat();
                EnsureAudioMonitor();
                SendPresence(PresenceOpcode.Hello, true);
                Log("[INFO] Connected to MultiCom services.");

                if (frameSource == null && availableCameras.Count > 0)
                {
                    StartCameraCapture();
                }
            }
            catch (Exception ex)
            {
                Log("[ERROR] Unable to start connection: " + ex.Message);
                StopNetworking("[ERROR] Connection aborted.");
            }
        }

        private void OnConnect(object sender, EventArgs e)
        {
            StartNetworking();
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            StopNetworking("[INFO] Disconnected by user.");
        }

        private void StopNetworking(string reason = null)
        {
            if (isDisconnecting)
            {
                return;
            }

            isDisconnecting = true;
            try
            {
                var logMessage = string.IsNullOrEmpty(reason) ? "[INFO] Disconnected." : reason;

                if (controlSender != null)
                {
                    SendPresence(PresenceOpcode.Goodbye, true);
                }

                StopHeartbeat();
                StopAudioMonitor();
                StopCameraCapture();

                CancelTokenSource(ref videoCts);
                CancelTokenSource(ref chatCts);
                CancelTokenSource(ref controlCts);

                CloseClient(ref chatSender);
                CloseClient(ref controlSender);
                CloseClient(ref videoSender);

                lock (presenceGate)
                {
                    presenceRecords.Clear();
                }

                ClearVideoTiles();
                UpdateMembersList();
                performanceTracker.Reset();

                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;

                serverReachable = false;
                lastServerSnapshotUtc = DateTime.MinValue;
                connectionStartedUtc = DateTime.MinValue;
                lastPresenceSentUtc = DateTime.MinValue;
                isSpeakingLocal = false;

                if (!suppressServerAlertReset)
                {
                    serverDisconnectNotified = false;
                }

                Log(logMessage);
            }
            finally
            {
                suppressServerAlertReset = false;
                isDisconnecting = false;
            }
        }

        private void LoadCameras()
        {
            availableCameras.Clear();
            foreach (Camera camera in CameraService.AvailableCameras)
            {
                availableCameras.Add(camera);
            }

            if (availableCameras.Count == 0)
            {
                selectedCameraIndex = -1;
                Log("[WARN] No cameras detected.");
            }
            else if (selectedCameraIndex < 0 || selectedCameraIndex >= availableCameras.Count)
            {
                selectedCameraIndex = 0;
            }

            UpdateCameraButtonState();
        }

        private void LoadAudioDevices()
        {
            cachedAudioDevices = AudioDeviceCatalog.EnumerateCaptureDevices();
            if (!string.IsNullOrEmpty(selectedAudioDeviceId) && cachedAudioDevices.All(d => !string.Equals(d.Id, selectedAudioDeviceId, StringComparison.OrdinalIgnoreCase)))
            {
                selectedAudioDeviceId = null;
            }
        }

        private void OnToggleCamera(object sender, EventArgs e)
        {
            if (frameSource != null)
            {
                StopCameraCapture();
            }
            else
            {
                StartCameraCapture();
            }
        }

        private Camera GetSelectedCamera()
        {
            if (selectedCameraIndex >= 0 && selectedCameraIndex < availableCameras.Count)
            {
                return availableCameras[selectedCameraIndex];
            }

            return null;
        }

        private static string GetCameraLabel(Camera camera)
        {
            return camera != null ? camera.ToString() : "Camera";
        }

        private void StartCameraCapture()
        {
            var selectedCamera = GetSelectedCamera();
            if (selectedCamera == null)
            {
                MessageBox.Show(this, "Select a camera in Settings before enabling broadcast.", "Camera unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (frameSource != null)
            {
                return;
            }

            try
            {
                cameraCts = new CancellationTokenSource();
                videoSender = CreateMulticastSender(true, videoEndpoint.Address);
                frameSource = new CameraFrameSource(selectedCamera);
                frameSource.Camera.CaptureWidth = captureWidth;
                frameSource.Camera.CaptureHeight = captureHeight;
                frameSource.Camera.Fps = captureFps;
                frameSource.NewFrame += OnCameraFrame;
                frameNumber = 0;
                frameSource.StartFrameCapture();
                UpdateCameraButtonState();
                SendPresence(PresenceOpcode.Heartbeat, true);
                Log("[INFO] Camera broadcasting enabled.");
            }
            catch (Exception ex)
            {
                Log("[ERROR] Camera start: " + ex.Message);
                StopCameraCapture();
            }
        }

        private void StopCameraCapture()
        {
            if (cameraCts != null)
            {
                cameraCts.Cancel();
                cameraCts.Dispose();
                cameraCts = null;
            }

            if (frameSource != null)
            {
                frameSource.NewFrame -= OnCameraFrame;
                frameSource.StopFrameCapture();
                frameSource = null;
            }

            CloseClient(ref videoSender);
            frameNumber = 0;
            UpdateCameraButtonState();
            SendPresence(PresenceOpcode.Heartbeat, true);
        }

        private void UpdateCameraButtonState()
        {
            if (btnToggleCamera == null)
            {
                return;
            }

            var broadcasting = frameSource != null;
            var hasCamera = availableCameras.Count > 0 || broadcasting;
            btnToggleCamera.Enabled = hasCamera;
            btnToggleCamera.Text = broadcasting ? "Disable camera" : "Enable camera";
            btnToggleCamera.BackColor = broadcasting ? Color.FromArgb(240, 71, 71) : Color.FromArgb(67, 181, 129);
        }

        private void EnsureAudioMonitor()
        {
            if (audioMonitor != null)
            {
                return;
            }

            try
            {
                audioMonitor = new AudioLevelMonitor(selectedAudioDeviceId);
                audioMonitor.LevelAvailable += OnAudioLevel;
            }
            catch (Exception ex)
            {
                Log("[WARN] Audio detection unavailable: " + ex.Message);
                audioMonitor = null;
            }
        }

        private void StopAudioMonitor()
        {
            if (audioMonitor != null)
            {
                audioMonitor.LevelAvailable -= OnAudioLevel;
                audioMonitor.Dispose();
                audioMonitor = null;
            }

            if (isSpeakingLocal)
            {
                isSpeakingLocal = false;
                ApplySpeakingVisual(clientId, false);
            }
        }

        private void OnAudioLevel(object sender, float level)
        {
            var speaking = level >= SPEAKING_THRESHOLD;
            if (speaking == isSpeakingLocal)
            {
                return;
            }

            isSpeakingLocal = speaking;
            SendPresence(PresenceOpcode.Heartbeat, true);
            ApplySpeakingVisual(clientId, speaking);
        }

        private void OnCameraFrame(Touchless.Vision.Contracts.IFrameSource source, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            if (cameraCts == null || cameraCts.IsCancellationRequested)
            {
                return;
            }

            Bitmap snapshot;
            try
            {
                snapshot = (Bitmap)frame.Image.Clone();
            }
            catch
            {
                return;
            }

            Bitmap preview;
            try
            {
                preview = (Bitmap)snapshot.Clone();
            }
            catch
            {
                snapshot.Dispose();
                return;
            }

            BeginInvoke(new Action(() => RenderFrame(clientId, GetDisplayName(), preview)));
            Task.Run(() => BroadcastFrameAsync(snapshot, cameraCts.Token));
        }

        private async Task BroadcastFrameAsync(Bitmap bitmap, CancellationToken token)
        {
            if (videoSender == null)
            {
                bitmap.Dispose();
                return;
            }

            try
            {
                byte[] buffer;
                using (var resized = new Bitmap(bitmap, new Size(captureWidth, captureHeight)))
                {
                    buffer = EncodeJpeg(resized);
                }

                bitmap.Dispose();
                var endpoint = videoEndpoint;
                var totalSegments = (int)Math.Ceiling((double)buffer.Length / PAYLOAD_BYTES);
                var timestamp = DateTime.UtcNow.Ticks;
                for (var segment = 0; segment < totalSegments; segment++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    var offset = segment * PAYLOAD_BYTES;
                    var chunkLength = Math.Min(PAYLOAD_BYTES, buffer.Length - offset);
                    var chunk = new byte[chunkLength];
                    Buffer.BlockCopy(buffer, offset, chunk, 0, chunkLength);
                    var header = new VideoPacketHeader(clientId, frameNumber, segment, totalSegments, chunkLength, timestamp, CalculateHash(chunk));
                    var headerBytes = header.ToByteArray();
                    var datagram = new byte[headerBytes.Length + chunkLength];
                    Buffer.BlockCopy(headerBytes, 0, datagram, 0, headerBytes.Length);
                    Buffer.BlockCopy(chunk, 0, datagram, headerBytes.Length, chunkLength);
                    await videoSender.SendAsync(datagram, datagram.Length, endpoint).ConfigureAwait(false);
                }

                performanceTracker.RegisterFrame(DateTime.UtcNow, 0);
                frameNumber++;
            }
            catch (Exception ex)
            {
                Log("[ERROR] Broadcast: " + ex.Message);
            }
        }

        private static int CalculateHash(byte[] payload)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < payload.Length; i += Math.Max(1, payload.Length / 32))
                {
                    hash = hash * 31 + payload[i];
                }

                return hash;
            }
        }

        private void StartHeartbeat()
        {
            if (heartbeatTimer != null)
            {
                return;
            }

            heartbeatTimer = new System.Threading.Timer(_ => SendPresence(PresenceOpcode.Heartbeat), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private void StopHeartbeat()
        {
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Dispose();
                heartbeatTimer = null;
            }
        }

        private void SendPresence(PresenceOpcode opcode, bool force = false)
        {
            if (controlSender == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (!force && lastPresenceSentUtc != DateTime.MinValue && now - lastPresenceSentUtc < presenceMinInterval)
            {
                return;
            }

            try
            {
                var envelope = PresenceMessage.CreateEvent(opcode, clientId, GetDisplayName(), frameSource != null, isSpeakingLocal, now);
                var payload = envelope.ToPacket();
                controlSender.Send(payload, payload.Length, controlEndpoint);
                if (serverUnicastAddress != null)
                {
                    var unicast = new IPEndPoint(serverUnicastAddress, controlEndpoint.Port);
                    controlSender.Send(payload, payload.Length, unicast);
                }

                lastPresenceSentUtc = now;
            }
            catch (Exception ex)
            {
                Log("[ERROR] Presence send: " + ex.Message);
            }
        }

        private string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(displayName) ? "Anon" : displayName;
        }

        private void UpdateProfileLabel()
        {
            if (lblProfileName != null)
            {
                lblProfileName.Text = string.Format("Signed in as {0}", GetDisplayName());
            }
        }

        private string ResolveDisplayName(Guid senderId)
        {
            if (senderId == clientId)
            {
                return GetDisplayName();
            }

            lock (presenceGate)
            {
                PresenceRecord record;
                if (presenceRecords.TryGetValue(senderId, out record))
                {
                    return record.DisplayName;
                }
            }

            return "Anon";
        }

        private void ApplySnapshot(PresenceMessage snapshot)
        {
            lastServerSnapshotUtc = DateTime.UtcNow;
            if (!serverReachable)
            {
                serverReachable = true;
                Log("[INFO] Synchronized with server.");
            }

            lock (presenceGate)
            {
                presenceRecords.Clear();
                foreach (var record in snapshot.Snapshot)
                {
                    presenceRecords[record.ClientId] = record;
                }
            }

            BeginInvoke(new Action(() =>
            {
                UpdateMembersList();
                SyncVideoTiles();
            }));
        }

        private void UpdateMembersList()
        {
            if (listMembers == null)
            {
                return;
            }

            listMembers.Items.Clear();
            lock (presenceGate)
            {
                foreach (var record in presenceRecords.Values.OrderBy(r => r.DisplayName))
                {
                    string prefix;
                    if (record.IsSpeaking)
                    {
                        prefix = "🔊";
                    }
                    else if (record.CameraEnabled)
                    {
                        prefix = "🎥";
                    }
                    else
                    {
                        prefix = "🙈";
                    }

                    listMembers.Items.Add(string.Format("{0} {1}", prefix, record.DisplayName));
                }
            }
        }

        private void SyncVideoTiles()
        {
            Dictionary<Guid, PresenceRecord> snapshot;
            lock (presenceGate)
            {
                snapshot = presenceRecords
                    .Where(p => p.Value.CameraEnabled)
                    .ToDictionary(p => p.Key, p => p.Value);
            }

            var stale = videoTiles.Keys.Where(id => !snapshot.ContainsKey(id)).ToList();
            foreach (var id in stale)
            {
                DisposeTile(id);
            }

            foreach (var pair in snapshot)
            {
                EnsureVideoTile(pair.Key, pair.Value.DisplayName);
                ApplySpeakingVisual(pair.Key, pair.Value.IsSpeaking);
            }
        }

        private VideoTile EnsureVideoTile(Guid senderId, string displayName)
        {
            VideoTile tile;
            if (!videoTiles.TryGetValue(senderId, out tile))
            {
                tile = new VideoTile(displayName);
                videoTiles[senderId] = tile;
                if (flowVideo != null)
                {
                    flowVideo.Controls.Add(tile.Container);
                }
            }

            tile.Caption.Text = displayName;
            var speaking = ResolveSpeakingState(senderId);
            ApplySpeakingVisual(senderId, speaking);
            return tile;
        }

        private void DisposeTile(Guid senderId)
        {
            VideoTile tile;
            if (!videoTiles.TryGetValue(senderId, out tile))
            {
                return;
            }

            if (tile.Surface.Image != null)
            {
                tile.Surface.Image.Dispose();
            }

            if (flowVideo != null)
            {
                flowVideo.Controls.Remove(tile.Container);
            }

            tile.Container.Dispose();
            videoTiles.Remove(senderId);
        }

        private void ClearVideoTiles()
        {
            var ids = videoTiles.Keys.ToArray();
            foreach (var id in ids)
            {
                DisposeTile(id);
            }

            videoTiles.Clear();
            if (flowVideo != null)
            {
                flowVideo.Controls.Clear();
            }
        }

        private void ListenVideoLoop(CancellationToken token)
        {
            var endpoint = videoEndpoint;
            using (var udp = CreateMulticastListener(endpoint))
            {
                udp.Client.ReceiveTimeout = 1000;
                var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
                while (!token.IsCancellationRequested)
                {
                    byte[] buffer;
                    try
                    {
                        buffer = udp.Receive(ref remote);
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        Log("[ERROR] Video listener: " + ex.Message);
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    VideoPacket packet;
                    if (!VideoPacket.TryParse(buffer, buffer.Length, out packet))
                    {
                        performanceTracker.RegisterLoss(1);
                        continue;
                    }

                    if (packet.Header.SenderId == clientId)
                    {
                        continue;
                    }

                    var receivedAt = DateTime.UtcNow;
                    byte[] frameBytes;
                    int lostSegments;
                    if (frameAssembler.TryAdd(packet, receivedAt, out frameBytes, out lostSegments))
                    {
                        if (lostSegments > 0)
                        {
                            performanceTracker.RegisterLoss(lostSegments);
                        }

                        try
                        {
                            using (var ms = new MemoryStream(frameBytes))
                            {
                                var bitmap = new Bitmap(ms);
                                var senderName = ResolveDisplayName(packet.Header.SenderId);
                                BeginInvoke(new Action(() => RenderFrame(packet.Header.SenderId, senderName, bitmap)));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("[ERROR] Frame decode: " + ex.Message);
                            performanceTracker.RegisterLoss(1);
                        }

                        var latencyMs = (receivedAt - new DateTime(packet.Header.TimestampTicks, DateTimeKind.Utc)).TotalMilliseconds;
                        performanceTracker.RegisterFrame(receivedAt, latencyMs);
                    }
                    else if (lostSegments > 0)
                    {
                        performanceTracker.RegisterLoss(lostSegments);
                    }
                }
            }
        }

        private void ListenChatLoop(CancellationToken token)
        {
            var endpoint = chatEndpoint;
            using (var udp = CreateMulticastListener(endpoint))
            {
                udp.Client.ReceiveTimeout = 1000;
                var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
                while (!token.IsCancellationRequested)
                {
                    byte[] buffer;
                    try
                    {
                        buffer = udp.Receive(ref remote);
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        Log("[ERROR] Chat listener: " + ex.Message);
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    ChatEnvelope envelope;
                    if (!ChatEnvelope.TryParse(buffer, out envelope))
                    {
                        continue;
                    }

                    if (envelope.SenderId == clientId)
                    {
                        continue;
                    }

                    BeginInvoke(new Action(() => AppendChat(envelope, false)));
                }
            }
        }

        private void ListenControlLoop(CancellationToken token)
        {
            var endpoint = controlEndpoint;
            using (var udp = CreateMulticastListener(endpoint))
            {
                udp.Client.ReceiveTimeout = 1000;
                var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
                while (!token.IsCancellationRequested)
                {
                    byte[] buffer;
                    try
                    {
                        buffer = udp.Receive(ref remote);
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        Log("[ERROR] Control listener: " + ex.Message);
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    PresenceMessage envelope;
                    if (!PresenceMessage.TryParse(buffer, out envelope))
                    {
                        continue;
                    }

                    if (envelope.Kind != PresenceOpcode.Snapshot)
                    {
                        continue;
                    }

                    ApplySnapshot(envelope);
                }
            }
        }

        private void RenderFrame(Guid senderId, string displayName, Bitmap bitmap)
        {
            var tile = EnsureVideoTile(senderId, displayName);
            if (tile.Surface.Image != null)
            {
                tile.Surface.Image.Dispose();
            }

            tile.Caption.Text = displayName;
            tile.Surface.Image = bitmap;
            var speaking = ResolveSpeakingState(senderId);
            ApplySpeakingVisual(senderId, speaking);
        }

        private bool ResolveSpeakingState(Guid senderId)
        {
            if (senderId == clientId)
            {
                return isSpeakingLocal;
            }

            lock (presenceGate)
            {
                PresenceRecord record;
                if (presenceRecords.TryGetValue(senderId, out record))
                {
                    return record.IsSpeaking;
                }
            }

            return false;
        }

        private void ApplySpeakingVisual(Guid senderId, bool isSpeaking)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplySpeakingVisual(senderId, isSpeaking)));
                return;
            }

            VideoTile tile;
            if (!videoTiles.TryGetValue(senderId, out tile))
            {
                return;
            }

            tile.Container.BackColor = isSpeaking ? Color.FromArgb(88, 101, 242) : Color.FromArgb(47, 49, 54);
            tile.Container.Padding = isSpeaking ? new Padding(6) : new Padding(8);
            tile.Caption.ForeColor = isSpeaking ? Color.White : Color.LightGray;
        }

        private void AppendChat(ChatEnvelope envelope, bool isLocal)
        {
            var prefix = isLocal ? "You" : envelope.Sender;
            var line = string.Format("[{0:HH:mm}] {1}: {2}", envelope.TimestampUtc, prefix, envelope.Message);
            listChat.Items.Insert(0, line);
            while (listChat.Items.Count > 200)
            {
                listChat.Items.RemoveAt(listChat.Items.Count - 1);
            }
        }

        private void OnSendMessage(object sender, EventArgs e)
        {
            SendChat();
        }

        private void OnMessageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SendChat();
            }
        }

        private void SendChat()
        {
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var senderName = GetDisplayName();
            var envelope = new ChatEnvelope(clientId, senderName, text, DateTime.UtcNow);
            var payload = envelope.ToPacket();

            try
            {
                if (chatSender != null)
                {
                    chatSender.Send(payload, payload.Length, chatEndpoint);
                }

                AppendChat(envelope, true);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                Log("[ERROR] Chat send: " + ex.Message);
            }
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            var snapshot = performanceTracker.BuildSnapshot();
            lblFps.Text = string.Format("FPS: {0:F1}", snapshot.FramesPerSecond);
            lblLatency.Text = string.Format("Latency: {0:F1} ms", snapshot.AverageLatencyMs);
            lblJitter.Text = string.Format("Jitter: {0:F1} ms", snapshot.JitterMs);
            lblLoss.Text = string.Format("Loss: {0} pkts", snapshot.LostPackets);
            CheckServerHealth();
        }

        private void CheckServerHealth()
        {
            if (videoCts == null && chatCts == null && controlCts == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (serverReachable)
            {
                if (lastServerSnapshotUtc != DateTime.MinValue && now - lastServerSnapshotUtc > serverTimeoutWindow)
                {
                    HandleServerTimeout("Lost connection to the MultiCom server. The client has been disconnected.");
                }
            }
            else if (connectionStartedUtc != DateTime.MinValue && now - connectionStartedUtc > serverGraceWindow)
            {
                HandleServerTimeout("The MultiCom server is not reachable. Please ensure the server is running.");
            }
        }

        private void HandleServerTimeout(string message)
        {
            if (serverDisconnectNotified)
            {
                return;
            }

            serverDisconnectNotified = true;
            suppressServerAlertReset = true;
            StopNetworking("[WARN] " + message);
            suppressServerAlertReset = false;
            MessageBox.Show(this, message, "Server unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            serverDisconnectNotified = false;
        }

        private void CancelTokenSource(ref CancellationTokenSource source)
        {
            if (source == null)
            {
                return;
            }

            try
            {
                source.Cancel();
            }
            finally
            {
                source.Dispose();
                source = null;
            }
        }

        private void CloseClient(ref UdpClient client)
        {
            if (client == null)
            {
                return;
            }

            client.Close();
            client = null;
        }

        private UdpClient CreateMulticastListener(IPEndPoint endpoint)
        {
            var udp = new UdpClient(AddressFamily.InterNetwork);
            udp.ExclusiveAddressUse = false;
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, endpoint.Port));
            udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            JoinMulticastGroup(udp, endpoint.Address);
            return udp;
        }

        private UdpClient CreateMulticastSender(bool enableLoopback, IPAddress multicastAddress)
        {
            var client = new UdpClient(AddressFamily.InterNetwork);
            client.ExclusiveAddressUse = false;
            client.MulticastLoopback = enableLoopback;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
            if (preferredInterfaceAddress != null)
            {
                client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, preferredInterfaceAddress.GetAddressBytes());
                client.Client.Bind(new IPEndPoint(preferredInterfaceAddress, 0));
            }
            else
            {
                client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            }

            if (multicastAddress != null)
            {
                JoinMulticastGroup(client, multicastAddress);
            }

            return client;
        }

        private void JoinMulticastGroup(UdpClient client, IPAddress multicastAddress)
        {
            if (multicastAddress == null)
            {
                return;
            }

            if (preferredInterfaceAddress != null)
            {
                client.JoinMulticastGroup(multicastAddress, preferredInterfaceAddress);
            }
            else
            {
                client.JoinMulticastGroup(multicastAddress);
            }
        }

        private static IReadOnlyList<IPAddress> EnumerateLocalInterfaces()
        {
            var addresses = new List<IPAddress>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var ipProps = nic.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !addresses.Contains(unicast.Address))
                    {
                        addresses.Add(unicast.Address);
                    }
                }
            }

            return addresses;
        }

        private bool IsConnected()
        {
            return videoCts != null || chatCts != null || controlCts != null;
        }

        private ClientPreferences BuildPreferences()
        {
            return new ClientPreferences
            {
                DisplayName = displayName,
                CameraIndex = selectedCameraIndex,
                AudioDeviceId = selectedAudioDeviceId,
                CaptureWidth = captureWidth,
                CaptureHeight = captureHeight,
                CaptureFps = captureFps,
                JpegQuality = jpegQuality,
                PreferredInterface = preferredInterfaceAddress,
                ServerAddress = serverUnicastAddress,
            };
        }

        private void ApplyPreferences(ClientPreferences preferences)
        {
            if (preferences == null)
            {
                return;
            }

            displayName = string.IsNullOrWhiteSpace(preferences.DisplayName) ? "Agent" : preferences.DisplayName.Trim();
            UpdateProfileLabel();

            var interfaceChanged = !Equals(preferredInterfaceAddress, preferences.PreferredInterface);
            var serverChanged = !Equals(serverUnicastAddress, preferences.ServerAddress);

            selectedCameraIndex = availableCameras.Count == 0 ? -1 : Math.Max(0, Math.Min(preferences.CameraIndex, availableCameras.Count - 1));
            selectedAudioDeviceId = string.IsNullOrEmpty(preferences.AudioDeviceId) ? null : preferences.AudioDeviceId;
            captureWidth = Math.Max(160, Math.Min(1920, preferences.CaptureWidth));
            captureHeight = Math.Max(120, Math.Min(1080, preferences.CaptureHeight));
            captureFps = Math.Max(5, Math.Min(60, preferences.CaptureFps));
            jpegQuality = Math.Max(10, Math.Min(100, preferences.JpegQuality));
            preferredInterfaceAddress = preferences.PreferredInterface;
            serverUnicastAddress = preferences.ServerAddress;

            LoadAudioDevices();
            StopAudioMonitor();
            EnsureAudioMonitor();

            var broadcasting = frameSource != null;
            if (broadcasting)
            {
                StopCameraCapture();
                StartCameraCapture();
            }
            else
            {
                UpdateCameraButtonState();
            }

            if ((interfaceChanged || serverChanged) && IsConnected())
            {
                StopNetworking("[INFO] Applying network configuration...");
                StartNetworking();
            }
            else
            {
                SendPresence(PresenceOpcode.Heartbeat, true);
            }
        }

        private void OnOpenSettings(object sender, EventArgs e)
        {
            LoadCameras();
            LoadAudioDevices();
            var cameraNames = availableCameras.Select(GetCameraLabel).ToList();
            var interfaces = EnumerateLocalInterfaces();

            using (var dialog = new SettingsDialog(BuildPreferences(), cameraNames, cachedAudioDevices, interfaces))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    ApplyPreferences(dialog.Preferences);
                }
            }
        }

        private byte[] EncodeJpeg(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                if (JpegCodec != null)
                {
                    using (var encoderParams = new EncoderParameters(1))
                    {
                        var quality = Math.Max(10L, Math.Min(100L, jpegQuality));
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                        bitmap.Save(ms, JpegCodec, encoderParams);
                    }
                }
                else
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                }

                return ms.ToArray();
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message)));
                return;
            }

            listDiagnostics.Items.Insert(0, string.Format("{0:HH:mm:ss} {1}", DateTime.Now, message));
            while (listDiagnostics.Items.Count > 120)
            {
                listDiagnostics.Items.RemoveAt(listDiagnostics.Items.Count - 1);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopNetworking("[INFO] Closing application.");
            base.OnFormClosing(e);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiCom.Shared.Networking;

namespace MultiCom.Server
{
    public partial class ServerForm : Form
    {
        private readonly object rosterGate = new object();
        private readonly Dictionary<Guid, PresenceRecord> roster = new Dictionary<Guid, PresenceRecord>();
        private readonly SemaphoreSlim snapshotLock = new SemaphoreSlim(1, 1);
        private readonly IPEndPoint controlEndpoint = MulticastChannels.BuildControlEndpoint();
        private CancellationTokenSource presenceToken;
        private Task presenceTask;
        private UdpClient snapshotSender;
        private DateTime lastSnapshotUtc = DateTime.MinValue;
        private int snapshotsBroadcast;

        public ServerForm()
        {
            InitializeComponent();
        }

        private void OnFormLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            StartPresenceService();
            streamingTimer.Start();
        }

        private void ApplyDiscordPalette()
        {
            btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(114, 137, 218);
            btnStop.FlatAppearance.BorderColor = Color.FromArgb(114, 118, 125);
            btnStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 82, 90);
            btnRefreshCamera.FlatAppearance.BorderColor = Color.FromArgb(114, 118, 125);
            btnRefreshCamera.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 82, 90);
        }

        private async void OnRefreshCameraClick(object sender, EventArgs e)
        {
            await BroadcastSnapshotAsync();
        }

        private void OnStartStreaming(object sender, EventArgs e)
        {
            StartPresenceService();
        }

        private void OnStopStreaming(object sender, EventArgs e)
        {
            StopPresenceService();
        }

        private void StopPresenceService()
        {
            if (presenceToken == null)
            {
                return;
            }

            presenceToken.Cancel();
            try
            {
                if (presenceTask != null)
                {
                    presenceTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch (AggregateException)
            {
            }

            presenceTask = null;
            presenceToken.Dispose();
            presenceToken = null;

            if (snapshotSender != null)
            {
                snapshotSender.Close();
                snapshotSender = null;
            }

            lock (rosterGate)
            {
                roster.Clear();
            }

            snapshotsBroadcast = 0;
            lastSnapshotUtc = DateTime.MinValue;
            UpdateRosterList();
            UpdateMetrics();
            Log("[INFO] Presence service stopped.");
        }

        private void StartPresenceService()
        {
            if (presenceToken != null)
            {
                Log("[WARN] Presence service already running.");
                return;
            }

            presenceToken = new CancellationTokenSource();
            presenceTask = Task.Run(() => ListenPresenceLoop(presenceToken.Token));
            snapshotSender = new UdpClient(AddressFamily.InterNetwork);
            snapshotSender.ExclusiveAddressUse = false;
            snapshotSender.MulticastLoopback = true;
            snapshotSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            snapshotSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            snapshotSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
            snapshotSender.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            snapshotSender.JoinMulticastGroup(controlEndpoint.Address);
            lock (rosterGate)
            {
                roster.Clear();
            }

            snapshotsBroadcast = 0;
            lastSnapshotUtc = DateTime.MinValue;
            UpdateRosterList();
            UpdateMetrics();
            Log("[INFO] Presence service started.");
            var _ = BroadcastSnapshotAsync();
        }

        private void ListenPresenceLoop(CancellationToken token)
        {
            var endpoint = MulticastChannels.BuildControlEndpoint();
            using (var udp = CreateMulticastListener(endpoint))
            {
                udp.JoinMulticastGroup(endpoint.Address);
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

                        Log("[ERROR] Control loop: " + ex.Message);
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    PresenceMessage message;
                    if (!PresenceMessage.TryParse(buffer, out message))
                    {
                        continue;
                    }

                    if (message.Kind == PresenceOpcode.Snapshot)
                    {
                        continue;
                    }

                    var changed = ApplyPresence(message);
                    if (changed)
                    {
                        var _ = BroadcastSnapshotAsync();
                    }
                }
            }
        }

        private static UdpClient CreateMulticastListener(IPEndPoint endpoint)
        {
            var udp = new UdpClient(AddressFamily.InterNetwork);
            udp.ExclusiveAddressUse = false;
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, endpoint.Port));
            return udp;
        }

        private bool ApplyPresence(PresenceMessage message)
        {
            var now = DateTime.UtcNow;
            var changed = false;
            lock (rosterGate)
            {
                switch (message.Kind)
                {
                    case PresenceOpcode.Hello:
                    case PresenceOpcode.Heartbeat:
                        PresenceRecord existing;
                        if (!roster.TryGetValue(message.ClientId, out existing))
                        {
                            changed = true;
                        }
                        else if (!string.Equals(existing.DisplayName, message.DisplayName, StringComparison.OrdinalIgnoreCase) || existing.CameraEnabled != message.CameraEnabled || existing.IsSpeaking != message.IsSpeaking)
                        {
                            changed = true;
                        }

                        roster[message.ClientId] = new PresenceRecord(message.ClientId, message.DisplayName, message.CameraEnabled, message.IsSpeaking, now);
                        break;
                    case PresenceOpcode.Goodbye:
                        changed = roster.Remove(message.ClientId);
                        break;
                }
            }

            if (changed)
            {
                BeginInvoke(new Action(UpdateRosterList));
                Log(string.Format("[INFO] Presence updated by {0}", message.DisplayName));
            }

            return changed;
        }

        private async Task BroadcastSnapshotAsync()
        {
            if (snapshotSender == null)
            {
                return;
            }

            await snapshotLock.WaitAsync().ConfigureAwait(false);
            try
            {
                PresenceRecord[] snapshot;
                lock (rosterGate)
                {
                    snapshot = roster.Values.ToArray();
                }

                var envelope = PresenceMessage.CreateSnapshot(snapshot);
                var payload = envelope.ToPacket();
                await snapshotSender.SendAsync(payload, payload.Length, controlEndpoint).ConfigureAwait(false);
                lastSnapshotUtc = DateTime.UtcNow;
                snapshotsBroadcast++;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log("[ERROR] Snapshot broadcast: " + ex.Message);
            }
            finally
            {
                snapshotLock.Release();
            }
        }

        private bool CleanupInactiveClients()
        {
            var removed = new List<Guid>();
            var now = DateTime.UtcNow;
            lock (rosterGate)
            {
                foreach (var pair in roster)
                {
                    if (now - pair.Value.LastSeenUtc > TimeSpan.FromSeconds(12))
                    {
                        removed.Add(pair.Key);
                    }
                }

                foreach (var clientId in removed)
                {
                    roster.Remove(clientId);
                }
            }

            if (removed.Count > 0)
            {
                BeginInvoke(new Action(UpdateRosterList));
                Log(string.Format("[WARN] Removed {0} inactive clients.", removed.Count));
            }

            return removed.Count > 0;
        }

        private void UpdateRosterList()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateRosterList));
                return;
            }

            if (listClients == null)
            {
                return;
            }

            listClients.BeginUpdate();
            listClients.Items.Clear();
            IEnumerable<PresenceRecord> ordered;
            lock (rosterGate)
            {
                ordered = roster.Values.OrderBy(r => r.DisplayName).ToArray();
            }

            foreach (var record in ordered)
            {
                var status = record.CameraEnabled
                    ? (record.IsSpeaking ? "Speaking" : "Camera ON")
                    : "Camera OFF";
                listClients.Items.Add(string.Format("{0} — {1}", record.DisplayName, status));
            }

            listClients.EndUpdate();
        }

        private async void OnMetricsTick(object sender, EventArgs e)
        {
            var removed = CleanupInactiveClients();
            if (removed || ShouldPushHeartbeat())
            {
                await BroadcastSnapshotAsync().ConfigureAwait(false);
            }

            UpdateMetrics();
        }

        private bool ShouldPushHeartbeat()
        {
            if (snapshotSender == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return lastSnapshotUtc == DateTime.MinValue || now - lastSnapshotUtc >= TimeSpan.FromSeconds(3);
        }

        private void UpdateMetrics()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateMetrics));
                return;
            }

            var now = DateTime.UtcNow;
            var lastSnapshotText = lastSnapshotUtc == DateTime.MinValue ? "-" : string.Format("{0:F1}s ago", (now - lastSnapshotUtc).TotalSeconds);
            var onlineCount = 0;
            lock (rosterGate)
            {
                onlineCount = roster.Count;
            }

            if (lblFrames != null)
            {
                lblFrames.Text = string.Format("Online: {0}", onlineCount);
            }

            if (lblBitrate != null)
            {
                lblBitrate.Text = string.Format("Snapshots: {0}", snapshotsBroadcast);
            }

            if (lblErrors != null)
            {
                lblErrors.Text = string.Format("Last push: {0}", lastSnapshotText);
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message)));
                return;
            }

            listEvents.Items.Insert(0, string.Format("{0:HH:mm:ss} {1}", DateTime.Now, message));
            while (listEvents.Items.Count > 100)
            {
                listEvents.Items.RemoveAt(listEvents.Items.Count - 1);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPresenceService();
            base.OnFormClosing(e);
        }
    }
}

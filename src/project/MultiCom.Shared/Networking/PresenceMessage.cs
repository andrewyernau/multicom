using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MultiCom.Shared.Networking
{
    public enum PresenceOpcode
    {
        Hello = 1,
        Goodbye = 2,
        Heartbeat = 3,
        Snapshot = 4
    }

    public sealed class PresenceRecord
    {
        public PresenceRecord(Guid clientId, string displayName, bool cameraEnabled, bool isSpeaking, DateTime lastSeenUtc)
        {
            ClientId = clientId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Anon" : displayName.Trim();
            CameraEnabled = cameraEnabled;
            IsSpeaking = isSpeaking;
            LastSeenUtc = lastSeenUtc;
        }

        public Guid ClientId { get; private set; }
        public string DisplayName { get; private set; }
        public bool CameraEnabled { get; private set; }
        public bool IsSpeaking { get; private set; }
        public DateTime LastSeenUtc { get; private set; }
    }

    public sealed class PresenceMessage
    {
        private PresenceMessage(PresenceOpcode kind, Guid clientId, string displayName, bool cameraEnabled, bool isSpeaking, DateTime timestampUtc, IReadOnlyList<PresenceRecord> snapshot)
        {
            Kind = kind;
            ClientId = clientId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Anon" : displayName.Trim();
            CameraEnabled = cameraEnabled;
            IsSpeaking = isSpeaking;
            TimestampUtc = timestampUtc;
            Snapshot = snapshot ?? Array.Empty<PresenceRecord>();
        }

        public PresenceOpcode Kind { get; private set; }
        public Guid ClientId { get; private set; }
        public string DisplayName { get; private set; }
        public bool CameraEnabled { get; private set; }
        public bool IsSpeaking { get; private set; }
        public DateTime TimestampUtc { get; private set; }
        public IReadOnlyList<PresenceRecord> Snapshot { get; private set; }

        public static PresenceMessage CreateEvent(PresenceOpcode kind, Guid clientId, string displayName, bool cameraEnabled, bool isSpeaking, DateTime timestampUtc)
        {
            if (kind == PresenceOpcode.Snapshot)
            {
                throw new ArgumentException("[AGENT] Snapshot events must include records.", "kind");
            }

            return new PresenceMessage(kind, clientId, displayName, cameraEnabled, isSpeaking, timestampUtc, null);
        }

        public static PresenceMessage CreateSnapshot(IEnumerable<PresenceRecord> records)
        {
            var snapshot = records == null ? Array.Empty<PresenceRecord>() : records.ToArray();
            return new PresenceMessage(PresenceOpcode.Snapshot, Guid.Empty, "Server", false, false, DateTime.UtcNow, snapshot);
        }

        public byte[] ToPacket()
        {
            var builder = new StringBuilder();
            builder.Append((int)Kind)
                   .Append('|')
                   .Append(TimestampUtc.ToString("o"))
                   .Append('|')
                   .Append(ClientId)
                   .Append('|')
                   .Append(Sanitize(DisplayName))
                   .Append('|')
                   .Append(CameraEnabled ? '1' : '0')
                   .Append('|')
                   .Append(IsSpeaking ? '1' : '0')
                   .Append('|');

            if (Snapshot.Count > 0)
            {
                var encoded = Snapshot.Select(record => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}^{1}^{2}^{3}^{4:o}",
                    record.ClientId,
                    Sanitize(record.DisplayName),
                    record.CameraEnabled ? '1' : '0',
                    record.IsSpeaking ? '1' : '0',
                    record.LastSeenUtc));
                builder.Append(string.Join(";", encoded));
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        public static bool TryParse(byte[] buffer, out PresenceMessage message)
        {
            message = null;
            if (buffer == null)
            {
                return false;
            }

            var content = Encoding.UTF8.GetString(buffer).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var parts = content.Split(new[] { '|' }, 7);
            if (parts.Length < 6)
            {
                return false;
            }

            int opcodeValue;
            if (!int.TryParse(parts[0], out opcodeValue))
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(PresenceOpcode), opcodeValue))
            {
                return false;
            }

            var kind = (PresenceOpcode)opcodeValue;
            DateTime timestamp;
            if (!DateTime.TryParse(parts[1], null, DateTimeStyles.RoundtripKind, out timestamp))
            {
                return false;
            }

            Guid clientId;
            if (!Guid.TryParse(parts[2], out clientId))
            {
                return false;
            }

            var displayName = parts[3];
            var cameraEnabled = parts[4] == "1";
            var isSpeaking = parts[5] == "1";
            var snapshot = ParseSnapshot(parts.Length == 7 ? parts[6] : null);
            message = new PresenceMessage(kind, clientId, displayName, cameraEnabled, isSpeaking, DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), snapshot);
            return true;
        }

        private static IReadOnlyList<PresenceRecord> ParseSnapshot(string snapshotPayload)
        {
            if (string.IsNullOrWhiteSpace(snapshotPayload))
            {
                return Array.Empty<PresenceRecord>();
            }

            var records = new List<PresenceRecord>();
            var entries = snapshotPayload.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var tokens = entry.Split('^');
                if (tokens.Length != 5)
                {
                    continue;
                }

                Guid clientId;
                if (!Guid.TryParse(tokens[0], out clientId))
                {
                    continue;
                }

                var displayName = tokens[1];
                var cameraEnabled = tokens[2] == "1";
                var speaking = tokens[3] == "1";
                DateTime lastSeen;
                if (!DateTime.TryParse(tokens[4], null, DateTimeStyles.RoundtripKind, out lastSeen))
                {
                    continue;
                }

                records.Add(new PresenceRecord(clientId, displayName, cameraEnabled, speaking, DateTime.SpecifyKind(lastSeen, DateTimeKind.Utc)));
            }

            return records;
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }

            return input.Replace("|", " ").Replace(";", " ").Replace("^", " ");
        }
    }
}

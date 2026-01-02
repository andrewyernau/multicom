using System;
using System.Globalization;
using System.Text;

namespace MultiCom.Shared.Chat
{
    public sealed class ChatEnvelope
    {
        public ChatEnvelope(Guid senderId, string sender, string message, DateTime timestampUtc)
            : this(senderId, sender, message, timestampUtc, Guid.NewGuid())
        {
        }

        public ChatEnvelope(Guid senderId, string sender, string message, DateTime timestampUtc, Guid messageId)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("[AGENT] Message cannot be empty.", nameof(message));
            }

            this.senderId = senderId;
            this.sender = string.IsNullOrWhiteSpace(sender) ? "Anon" : sender.Trim();
            this.message = message.Trim();
            this.timestampUtc = timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc : timestampUtc.ToUniversalTime();
            this.messageId = messageId == Guid.Empty ? Guid.NewGuid() : messageId;
        }

        private readonly Guid senderId;
        private readonly string sender;
        private readonly string message;
        private readonly DateTime timestampUtc;
        private readonly Guid messageId;

        public Guid SenderId { get { return senderId; } }
        public string Sender { get { return sender; } }
        public string Message { get { return message; } }
        public DateTime TimestampUtc { get { return timestampUtc; } }
        public Guid MessageId { get { return messageId; } }

        public byte[] ToPacket()
        {
            var payload = string.Format(CultureInfo.InvariantCulture, "{0:o}|{1}|{2}|{3}|{4}", TimestampUtc, SenderId, MessageId, Sender, Message);
            return Encoding.UTF8.GetBytes(payload);
        }

        public static bool TryParse(byte[] buffer, out ChatEnvelope envelope)
        {
            envelope = null;
            if (buffer == null)
            {
                return false;
            }

            var content = Encoding.UTF8.GetString(buffer).Trim();
            var parts = content.Split(new[] { '|' }, 5);
            if (parts.Length != 5)
            {
                return false;
            }

            DateTime timestamp;
            if (!DateTime.TryParseExact(parts[0], "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp))
            {
                return false;
            }

            Guid senderId;
            if (!Guid.TryParse(parts[1], out senderId))
            {
                return false;
            }

            Guid messageId;
            if (!Guid.TryParse(parts[2], out messageId))
            {
                messageId = Guid.Empty;
            }

            envelope = new ChatEnvelope(senderId, parts[3], parts[4], timestamp.Kind == DateTimeKind.Utc ? timestamp : timestamp.ToUniversalTime(), messageId);
            return true;
        }
    }
}

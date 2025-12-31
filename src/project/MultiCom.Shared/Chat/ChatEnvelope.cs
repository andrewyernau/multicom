using System;
using System.Text;

namespace MultiCom.Shared.Chat
{
    public sealed class ChatEnvelope
    {
        public ChatEnvelope(Guid senderId, string sender, string message, DateTime timestampUtc)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("[AGENT] Message cannot be empty.", "message");
            }

            this.senderId = senderId;
            this.sender = string.IsNullOrWhiteSpace(sender) ? "Anon" : sender.Trim();
            this.message = message.Trim();
            this.timestampUtc = timestampUtc;
        }

        private readonly Guid senderId;
        private readonly string sender;
        private readonly string message;
        private readonly DateTime timestampUtc;

        public Guid SenderId { get { return senderId; } }
        public string Sender { get { return sender; } }
        public string Message { get { return message; } }
        public DateTime TimestampUtc { get { return timestampUtc; } }

        public byte[] ToPacket()
        {
            var payload = string.Format("{0:o}|{1}|{2}|{3}", TimestampUtc, SenderId, Sender, Message);
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
            var parts = content.Split(new[] { '|' }, 4);
            if (parts.Length != 4)
            {
                return false;
            }

            DateTime timestamp;
            if (!DateTime.TryParse(parts[0], out timestamp))
            {
                return false;
            }

            Guid senderId;
            if (!Guid.TryParse(parts[1], out senderId))
            {
                return false;
            }

            envelope = new ChatEnvelope(senderId, parts[2], parts[3], DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
            return true;
        }
    }
}

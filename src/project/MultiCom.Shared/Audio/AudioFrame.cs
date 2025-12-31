using System;

namespace MultiCom.Shared.Audio
{
    public sealed class AudioFrame
    {
        private const int SIGNATURE = 0x57414C41; // "WALA"
        private const int HEADER_SIZE = 20;

        public AudioFrame(int sequenceNumber, byte[] payload, long timestampTicks)
        {
            if (payload == null)
            {
                throw new ArgumentNullException("payload");
            }

            this.sequenceNumber = sequenceNumber;
            this.payload = payload;
            this.timestampTicks = timestampTicks;
        }

        private readonly int sequenceNumber;
        private readonly byte[] payload;
        private readonly long timestampTicks;

        public int SequenceNumber { get { return sequenceNumber; } }
        public byte[] Payload { get { return payload; } }
        public long TimestampTicks { get { return timestampTicks; } }

        public byte[] ToPacket()
        {
            var buffer = new byte[HEADER_SIZE + Payload.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(SIGNATURE), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(SequenceNumber), 0, buffer, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(TimestampTicks), 0, buffer, 8, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(Payload.Length), 0, buffer, 16, 4);
            Buffer.BlockCopy(Payload, 0, buffer, HEADER_SIZE, Payload.Length);
            return buffer;
        }

        public static bool TryParse(byte[] buffer, int bytesRead, out AudioFrame frame)
        {
            frame = null;
            if (buffer == null || bytesRead < HEADER_SIZE)
            {
                return false;
            }

            var signature = BitConverter.ToInt32(buffer, 0);
            if (signature != SIGNATURE)
            {
                return false;
            }

            var sequence = BitConverter.ToInt32(buffer, 4);
            var timestamp = BitConverter.ToInt64(buffer, 8);
            var payloadLength = BitConverter.ToInt32(buffer, 16);
            if (payloadLength < 0 || payloadLength > bytesRead - HEADER_SIZE)
            {
                return false;
            }

            var payload = new byte[payloadLength];
            Buffer.BlockCopy(buffer, HEADER_SIZE, payload, 0, payloadLength);
            frame = new AudioFrame(sequence, payload, timestamp);
            return true;
        }
    }
}

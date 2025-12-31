using System;

namespace MultiCom.Shared.Networking
{
    public sealed class VideoPacketHeader
    {
        public const int HEADER_SIZE = 48;
        private const int SIGNATURE = 0x564D434D; // "VMCM"

        private readonly Guid senderId;
        private readonly int frameNumber;
        private readonly int sequenceNumber;
        private readonly int totalSequences;
        private readonly int payloadLength;
        private readonly long timestampTicks;
        private readonly int payloadHash;

        public Guid SenderId { get { return senderId; } }
        public int FrameNumber { get { return frameNumber; } }
        public int SequenceNumber { get { return sequenceNumber; } }
        public int TotalSequences { get { return totalSequences; } }
        public int PayloadLength { get { return payloadLength; } }
        public long TimestampTicks { get { return timestampTicks; } }
        public int PayloadHash { get { return payloadHash; } }

        public VideoPacketHeader(Guid senderId, int frameNumber, int sequenceNumber, int totalSequences, int payloadLength, long timestampTicks, int payloadHash)
        {
            this.senderId = senderId;
            this.frameNumber = frameNumber;
            this.sequenceNumber = sequenceNumber;
            this.totalSequences = totalSequences;
            this.payloadLength = payloadLength;
            this.timestampTicks = timestampTicks;
            this.payloadHash = payloadHash;
        }

        public byte[] ToByteArray()
        {
            var buffer = new byte[HEADER_SIZE];
            WriteInt(buffer, 0, SIGNATURE);
            WriteInt(buffer, 4, FrameNumber);
            WriteInt(buffer, 8, SequenceNumber);
            WriteInt(buffer, 12, TotalSequences);
            WriteInt(buffer, 16, PayloadLength);
            WriteLong(buffer, 20, TimestampTicks);
            WriteInt(buffer, 28, PayloadHash);
            WriteGuid(buffer, 32, SenderId);
            return buffer;
        }

        public static bool TryParse(byte[] buffer, out VideoPacketHeader header)
        {
            header = null;
            if (buffer == null || buffer.Length < HEADER_SIZE)
            {
                return false;
            }

            var signature = ReadInt(buffer, 0);
            if (signature != SIGNATURE)
            {
                return false;
            }

            header = new VideoPacketHeader(
                ReadGuid(buffer, 32),
                ReadInt(buffer, 4),
                ReadInt(buffer, 8),
                ReadInt(buffer, 12),
                ReadInt(buffer, 16),
                ReadLong(buffer, 20),
                ReadInt(buffer, 28));
            return true;
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            var data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
        }

        private static void WriteLong(byte[] buffer, int offset, long value)
        {
            var data = BitConverter.GetBytes(value);
            Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
        }

        private static void WriteGuid(byte[] buffer, int offset, Guid value)
        {
            var data = value.ToByteArray();
            Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
        }

        private static int ReadInt(byte[] buffer, int offset)
        {
            return BitConverter.ToInt32(buffer, offset);
        }

        private static long ReadLong(byte[] buffer, int offset)
        {
            return BitConverter.ToInt64(buffer, offset);
        }

        private static Guid ReadGuid(byte[] buffer, int offset)
        {
            var data = new byte[16];
            Buffer.BlockCopy(buffer, offset, data, 0, data.Length);
            return new Guid(data);
        }
    }

    public sealed class VideoPacket
    {
        private readonly VideoPacketHeader header;
        private readonly byte[] payload;

        public VideoPacketHeader Header { get { return header; } }
        public byte[] Payload { get { return payload; } }

        public VideoPacket(VideoPacketHeader header, byte[] payload)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (payload == null)
            {
                throw new ArgumentNullException("payload");
            }

            this.header = header;
            this.payload = payload;
        }

        public static VideoPacket FromSegments(VideoPacketHeader header, ArraySegment<byte> payloadSegment)
        {
            var payload = new byte[payloadSegment.Count];
            Buffer.BlockCopy(payloadSegment.Array, payloadSegment.Offset, payload, 0, payload.Length);
            return new VideoPacket(header, payload);
        }

        public static bool TryParse(byte[] buffer, int length, out VideoPacket packet)
        {
            packet = null;
            if (length < VideoPacketHeader.HEADER_SIZE)
            {
                return false;
            }

            var headerBytes = new byte[VideoPacketHeader.HEADER_SIZE];
            Buffer.BlockCopy(buffer, 0, headerBytes, 0, VideoPacketHeader.HEADER_SIZE);
            VideoPacketHeader header;
            if (!VideoPacketHeader.TryParse(headerBytes, out header))
            {
                return false;
            }

            var payload = new byte[length - VideoPacketHeader.HEADER_SIZE];
            Buffer.BlockCopy(buffer, VideoPacketHeader.HEADER_SIZE, payload, 0, payload.Length);
            packet = new VideoPacket(header, payload);
            return true;
        }
    }
}

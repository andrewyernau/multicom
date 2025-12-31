using System;
using System.Collections.Generic;

namespace MultiCom.Shared.Networking
{
    public sealed class VideoFrameAssembler
    {
        private sealed class FrameSlot
        {
            private readonly VideoPacketHeader header;
            private readonly DateTime createdAt;
            private readonly int totalSegments;
            private readonly byte[][] segments;
            private int receivedSegments;

            public FrameSlot(VideoPacketHeader header)
            {
                this.header = header;
                createdAt = DateTime.UtcNow;
                totalSegments = Math.Max(1, Math.Min(header.TotalSequences, 256));
                segments = new byte[totalSegments][];
            }

            public VideoPacketHeader Header { get { return header; } }
            public DateTime CreatedAt { get { return createdAt; } }
            public int TotalSegments { get { return totalSegments; } }
            public byte[][] Segments { get { return segments; } }
            public int ReceivedSegments { get { return receivedSegments; } }

            public bool Push(VideoPacket packet)
            {
                if (packet.Header.SequenceNumber < 0 || packet.Header.SequenceNumber >= totalSegments)
                {
                    return false;
                }

                if (segments[packet.Header.SequenceNumber] != null)
                {
                    return false;
                }

                segments[packet.Header.SequenceNumber] = packet.Payload;
                receivedSegments++;
                return receivedSegments == totalSegments;
            }

            public byte[] BuildFrame()
            {
                var frameSize = 0;
                for (var i = 0; i < totalSegments; i++)
                {
                    if (segments[i] == null)
                    {
                        return null;
                    }

                    frameSize += segments[i].Length;
                }

                var frame = new byte[frameSize];
                var offset = 0;
                for (var i = 0; i < totalSegments; i++)
                {
                    Buffer.BlockCopy(segments[i], 0, frame, offset, segments[i].Length);
                    offset += segments[i].Length;
                }

                return frame;
            }

            public int CountMissing()
            {
                return totalSegments - receivedSegments;
            }
        }

        private readonly Dictionary<string, FrameSlot> inflightFrames = new Dictionary<string, FrameSlot>();
        private readonly object gate = new object();
        private readonly TimeSpan frameTimeout;

        public VideoFrameAssembler(TimeSpan? timeout = null)
        {
            frameTimeout = timeout ?? TimeSpan.FromMilliseconds(500);
        }

        public bool TryAdd(VideoPacket packet, DateTime receivedAtUtc, out byte[] frameBytes, out int lostSegments)
        {
            frameBytes = null;
            lostSegments = 0;
            lock (gate)
            {
                lostSegments = CleanupExpired(receivedAtUtc);

                var key = BuildKey(packet.Header);
                FrameSlot slot;
                if (!inflightFrames.TryGetValue(key, out slot))
                {
                    slot = new FrameSlot(packet.Header);
                    inflightFrames[key] = slot;
                }

                var completed = slot.Push(packet);
                if (!completed)
                {
                    return false;
                }

                frameBytes = slot.BuildFrame();
                inflightFrames.Remove(key);
                return frameBytes != null;
            }
        }

        private int CleanupExpired(DateTime nowUtc)
        {
            var expiredKeys = new List<string>();
            var lostSegments = 0;
            foreach (var pair in inflightFrames)
            {
                if (nowUtc - pair.Value.CreatedAt >= frameTimeout)
                {
                    expiredKeys.Add(pair.Key);
                    lostSegments += pair.Value.CountMissing();
                }
            }

            foreach (var key in expiredKeys)
            {
                inflightFrames.Remove(key);
            }

            return lostSegments;
        }

        private static string BuildKey(VideoPacketHeader header)
        {
            return string.Format("{0:N}:{1}", header.SenderId, header.FrameNumber);
        }
    }
}

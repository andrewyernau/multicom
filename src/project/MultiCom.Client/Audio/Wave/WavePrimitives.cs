using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Describes the PCM format used for capture and playback.
    /// </summary>
    internal sealed class WaveFormat
    {
        public WaveFormat(int sampleRate, int bitsPerSample, int channels)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "[AGENT] sampleRate must be positive.");
            }

            if (bitsPerSample != 8 && bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "[AGENT] bitsPerSample must be 8, 16, 24 or 32.");
            }

            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "[AGENT] channels must be positive.");
            }

            SampleRate = sampleRate;
            BitsPerSample = (short)bitsPerSample;
            Channels = (short)channels;
            BlockAlign = (short)(Channels * (BitsPerSample / 8));
            AverageBytesPerSecond = SampleRate * BlockAlign;
        }

        public short Channels { get; }

        public int SampleRate { get; }

        public short BitsPerSample { get; }

        public short BlockAlign { get; }

        public int AverageBytesPerSecond { get; }

        internal WaveFormatNative ToNative()
        {
            return new WaveFormatNative
            {
                wFormatTag = 1,
                nChannels = Channels,
                nSamplesPerSec = SampleRate,
                nAvgBytesPerSec = AverageBytesPerSecond,
                nBlockAlign = BlockAlign,
                wBitsPerSample = BitsPerSample,
                cbSize = 0
            };
        }
    }

    /// <summary>
    /// Wave provider contract.
    /// </summary>
    internal interface IWaveProvider
    {
        WaveFormat WaveFormat { get; }

        int Read(byte[] buffer, int offset, int count);
    }

    /// <summary>
    /// Playback states used by WaveOutEvent.
    /// </summary>
    internal enum PlaybackState
    {
        Stopped,
        Playing,
        Paused
    }

    /// <summary>
    /// Simple circular buffer implementation compatible with the original BufferedWaveProvider API.
    /// </summary>
    internal sealed class BufferedWaveProvider : IWaveProvider
    {
        private readonly byte[] buffer;
        private readonly object syncRoot = new object();
        private int readPosition;
        private int writePosition;
        private int bufferedBytes;

        public BufferedWaveProvider(WaveFormat waveFormat, int bufferMilliseconds = 500)
        {
            WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
            if (bufferMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferMilliseconds), "[AGENT] bufferMilliseconds must be positive.");
            }

            var bytesPerMillisecond = Math.Max(1, waveFormat.AverageBytesPerSecond / 1000);
            buffer = new byte[bytesPerMillisecond * bufferMilliseconds];
        }

        public WaveFormat WaveFormat { get; }

        public bool DiscardOnBufferOverflow { get; set; }

        public void AddSamples(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || offset >= data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || offset + count > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return;
            }

            lock (syncRoot)
            {
                if (count > buffer.Length)
                {
                    offset += count - buffer.Length;
                    count = buffer.Length;
                }

                var available = buffer.Length - bufferedBytes;
                if (count > available)
                {
                    if (DiscardOnBufferOverflow)
                    {
                        return;
                    }

                    // Drop the oldest data by advancing the read position.
                    var bytesToDrop = count - available;
                    readPosition = (readPosition + bytesToDrop) % buffer.Length;
                    bufferedBytes -= bytesToDrop;
                    if (bufferedBytes < 0)
                    {
                        bufferedBytes = 0;
                    }
                }

                var firstPart = Math.Min(count, buffer.Length - writePosition);
                Buffer.BlockCopy(data, offset, buffer, writePosition, firstPart);
                Buffer.BlockCopy(data, offset + firstPart, buffer, 0, count - firstPart);
                writePosition = (writePosition + count) % buffer.Length;
                bufferedBytes = Math.Min(buffer.Length, bufferedBytes + count);
            }
        }

        public int Read(byte[] destinationBuffer, int offset, int count)
        {
            if (destinationBuffer == null)
            {
                throw new ArgumentNullException(nameof(destinationBuffer));
            }

            if (offset < 0 || offset >= destinationBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || offset + count > destinationBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int bytesRead;
            lock (syncRoot)
            {
                bytesRead = Math.Min(count, bufferedBytes);
                var firstPart = Math.Min(bytesRead, buffer.Length - readPosition);
                Buffer.BlockCopy(buffer, readPosition, destinationBuffer, offset, firstPart);
                Buffer.BlockCopy(buffer, 0, destinationBuffer, offset + firstPart, bytesRead - firstPart);
                readPosition = (readPosition + bytesRead) % buffer.Length;
                bufferedBytes -= bytesRead;
            }

            if (bytesRead < count)
            {
                Array.Clear(destinationBuffer, offset + bytesRead, count - bytesRead);
            }

            return count;
        }
    }
}

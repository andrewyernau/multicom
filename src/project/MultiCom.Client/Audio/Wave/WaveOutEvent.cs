using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace NAudio.Wave
{
    /// <summary>
    /// Minimal WaveOut implementation compatible with the original WaveOutEvent API.
    /// </summary>
    internal sealed class WaveOutEvent : IDisposable
    {
        private readonly List<WaveOutBuffer> buffers = new List<WaveOutBuffer>();
        private AutoResetEvent bufferEvent;
        private WaveOutProc callbackDelegate;
        private Thread playbackThread;
        private IntPtr handle = IntPtr.Zero;
        private bool disposed;
        private bool stopRequested;

        public WaveOutEvent()
        {
            DeviceNumber = -1;
            DesiredLatency = 100;
            NumberOfBuffers = 2;
        }

        public int DeviceNumber { get; set; }

        public int DesiredLatency { get; set; }

        public int NumberOfBuffers { get; set; }

        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;

        public IWaveProvider WaveProvider { get; private set; }

        public void Init(IWaveProvider waveProvider)
        {
            WaveProvider = waveProvider ?? throw new ArgumentNullException(nameof(waveProvider));
        }

        public void Play()
        {
            ThrowIfDisposed();
            if (WaveProvider == null)
            {
                throw new InvalidOperationException("[AGENT] Call Init with a valid wave provider before Play.");
            }

            if (PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            EnsureOutput();
            stopRequested = false;

            if (playbackThread == null || !playbackThread.IsAlive)
            {
                playbackThread = new Thread(PlaybackLoop)
                {
                    IsBackground = true,
                    Name = "WaveOutEvent"
                };
                playbackThread.Start();
            }

            PlaybackState = PlaybackState.Playing;
            bufferEvent?.Set();
        }

        public void Stop()
        {
            if (PlaybackState == PlaybackState.Stopped)
            {
                return;
            }

            stopRequested = true;
            bufferEvent?.Set();
            playbackThread?.Join();
            playbackThread = null;

            if (handle != IntPtr.Zero)
            {
                WaveInterop.waveOutReset(handle);
            }

            PlaybackState = PlaybackState.Stopped;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            Stop();

            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }

            buffers.Clear();

            if (handle != IntPtr.Zero)
            {
                WaveInterop.waveOutClose(handle);
                handle = IntPtr.Zero;
            }

            bufferEvent?.Dispose();
            bufferEvent = null;
            disposed = true;
        }

        private void EnsureOutput()
        {
            if (handle != IntPtr.Zero)
            {
                return;
            }

            var format = WaveProvider?.WaveFormat ?? throw new InvalidOperationException("[AGENT] WaveFormat is required.");
            var nativeFormat = format.ToNative();
            var deviceId = DeviceNumber < 0 ? WaveInterop.WAVE_MAPPER : DeviceNumber;

            callbackDelegate = OnWaveOutProc;
            bufferEvent = new AutoResetEvent(false);

            WaveInterop.ThrowOnError(WaveInterop.waveOutOpen(out handle, deviceId, ref nativeFormat, callbackDelegate, IntPtr.Zero, WaveInterop.CALLBACK_FUNCTION), "waveOutOpen");
            CreateBuffers(format);
        }

        private void CreateBuffers(WaveFormat format)
        {
            foreach (var buffer in buffers)
            {
                buffer.Dispose();
            }

            buffers.Clear();

            var bytesPerBuffer = Math.Max(format.AverageBytesPerSecond * DesiredLatency / 1000, format.BlockAlign);
            bytesPerBuffer = Align(bytesPerBuffer, format.BlockAlign);

            for (var i = 0; i < Math.Max(1, NumberOfBuffers); i++)
            {
                buffers.Add(new WaveOutBuffer(handle, bytesPerBuffer));
            }
        }

        private void PlaybackLoop()
        {
            foreach (var buffer in buffers)
            {
                FillBuffer(buffer);
            }

            while (true)
            {
                if (stopRequested)
                {
                    return;
                }

                bufferEvent?.WaitOne();

                if (stopRequested)
                {
                    return;
                }

                foreach (var buffer in buffers)
                {
                    if (!buffer.InQueue)
                    {
                        FillBuffer(buffer);
                    }
                }
            }
        }

        private void FillBuffer(WaveOutBuffer buffer)
        {
            var data = buffer.Buffer;
            var bytesNeeded = data.Length;
            var read = WaveProvider.Read(data, 0, bytesNeeded);

            if (read < bytesNeeded)
            {
                Array.Clear(data, read, bytesNeeded - read);
            }

            buffer.Submit(bytesNeeded);
        }

        private void OnWaveOutProc(IntPtr hWaveOut, WaveOutMessage message, IntPtr dwInstance, IntPtr waveHeaderPtr, IntPtr reserved)
        {
            if (message == WaveOutMessage.Done)
            {
                bufferEvent?.Set();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WaveOutEvent));
            }
        }

        private static int Align(int value, int alignment)
        {
            if (alignment <= 0)
            {
                return value;
            }

            var remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        private sealed class WaveOutBuffer : IDisposable
        {
            private readonly IntPtr waveOutHandle;
            private readonly byte[] buffer;
            private readonly GCHandle bufferHandle;
            private readonly IntPtr headerPtr;
            private bool prepared;

            public WaveOutBuffer(IntPtr handle, int size)
            {
                waveOutHandle = handle;
                buffer = new byte[size];
                bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                var header = new WaveHeader
                {
                    lpData = bufferHandle.AddrOfPinnedObject(),
                    dwBufferLength = size,
                    dwBytesRecorded = 0,
                    dwUser = IntPtr.Zero,
                    dwFlags = 0,
                    dwLoops = 0,
                    lpNext = IntPtr.Zero,
                    reserved = IntPtr.Zero
                };

                headerPtr = Marshal.AllocHGlobal(WaveInterop.HeaderSize);
                Marshal.StructureToPtr(header, headerPtr, false);
                Prepare();
            }

            public byte[] Buffer => buffer;

            public bool InQueue
            {
                get
                {
                    var header = Marshal.PtrToStructure<WaveHeader>(headerPtr);
                    return (header.dwFlags & (int)WaveHeaderFlags.InQueue) == (int)WaveHeaderFlags.InQueue;
                }
            }

            public void Submit(int bytes)
            {
                var header = Marshal.PtrToStructure<WaveHeader>(headerPtr);
                header.dwBufferLength = bytes;
                header.dwBytesRecorded = bytes;
                header.dwFlags &= ~(int)WaveHeaderFlags.Done;
                Marshal.StructureToPtr(header, headerPtr, true);
                WaveInterop.ThrowOnError(WaveInterop.waveOutWrite(waveOutHandle, headerPtr, WaveInterop.HeaderSize), "waveOutWrite");
            }

            public void Dispose()
            {
                if (prepared)
                {
                    WaveInterop.ThrowOnError(WaveInterop.waveOutUnprepareHeader(waveOutHandle, headerPtr, WaveInterop.HeaderSize), "waveOutUnprepareHeader");
                    prepared = false;
                }

                Marshal.FreeHGlobal(headerPtr);
                if (bufferHandle.IsAllocated)
                {
                    bufferHandle.Free();
                }
            }

            private void Prepare()
            {
                WaveInterop.ThrowOnError(WaveInterop.waveOutPrepareHeader(waveOutHandle, headerPtr, WaveInterop.HeaderSize), "waveOutPrepareHeader");
                prepared = true;
            }
        }
    }
}

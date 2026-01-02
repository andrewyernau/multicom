using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Describes the capture device capabilities.
    /// </summary>
    internal readonly struct WaveInCapabilities
    {
        internal WaveInCapabilities(WaveInCaps caps)
        {
            ProductName = string.IsNullOrWhiteSpace(caps.szPname) ? "Unknown" : caps.szPname.Trim();
            Channels = caps.wChannels;
        }

        public string ProductName { get; }

        public short Channels { get; }
    }

    /// <summary>
    /// Provides access to WinMM input devices.
    /// </summary>
    internal static class WaveIn
    {
        public static int DeviceCount => WaveInterop.waveInGetNumDevs();

        public static WaveInCapabilities GetCapabilities(int deviceNumber)
        {
            if (deviceNumber < 0 || deviceNumber >= DeviceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceNumber), "[AGENT] Invalid audio capture device index.");
            }

            var caps = new WaveInCaps();
            var result = WaveInterop.waveInGetDevCaps(new IntPtr(deviceNumber), ref caps, Marshal.SizeOf(typeof(WaveInCaps)));
            WaveInterop.ThrowOnError(result, "waveInGetDevCaps");
            return new WaveInCapabilities(caps);
        }
    }

    /// <summary>
    /// Event data for recorded PCM buffers.
    /// </summary>
    internal sealed class WaveInEventArgs : EventArgs
    {
        public WaveInEventArgs(byte[] buffer, int bytesRecorded)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            BytesRecorded = bytesRecorded;
        }

        public byte[] Buffer { get; }

        public int BytesRecorded { get; }
    }

    /// <summary>
    /// Minimal WaveIn implementation using WinMM callbacks.
    /// </summary>
    internal sealed class WaveInEvent : IDisposable
    {
        private readonly Dictionary<IntPtr, WaveInBuffer> bufferLookup = new Dictionary<IntPtr, WaveInBuffer>();
        private readonly object bufferLock = new object();
        private WaveInProc callbackDelegate;
        private IntPtr handle = IntPtr.Zero;
        private bool disposed;
        private bool recording;

        public WaveInEvent()
        {
            DeviceNumber = -1;
            BufferMilliseconds = 50;
            NumberOfBuffers = 3;
            WaveFormat = new WaveFormat(8000, 16, 1);
        }

        public event EventHandler<WaveInEventArgs> DataAvailable;

        public int DeviceNumber { get; set; }

        public int BufferMilliseconds { get; set; }

        public int NumberOfBuffers { get; set; }

        public WaveFormat WaveFormat { get; set; }

        public void StartRecording()
        {
            ThrowIfDisposed();
            if (recording)
            {
                return;
            }

            if (BufferMilliseconds <= 0)
            {
                throw new InvalidOperationException("[AGENT] BufferMilliseconds must be positive.");
            }

            if (NumberOfBuffers <= 0)
            {
                throw new InvalidOperationException("[AGENT] NumberOfBuffers must be positive.");
            }

            var format = WaveFormat ?? throw new InvalidOperationException("[AGENT] WaveFormat must be specified.");
            var nativeFormat = format.ToNative();
            callbackDelegate = OnWaveInProc;

            var deviceId = DeviceNumber < 0 ? WaveInterop.WAVE_MAPPER : DeviceNumber;
            WaveInterop.ThrowOnError(WaveInterop.waveInOpen(out handle, deviceId, ref nativeFormat, callbackDelegate, IntPtr.Zero, WaveInterop.CALLBACK_FUNCTION), "waveInOpen");

            InitializeBuffers(format);
            WaveInterop.ThrowOnError(WaveInterop.waveInStart(handle), "waveInStart");
            recording = true;
        }

        public void StopRecording()
        {
            if (!recording)
            {
                return;
            }

            WaveInterop.waveInStop(handle);
            WaveInterop.waveInReset(handle);
            recording = false;
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

            StopRecording();

            if (handle != IntPtr.Zero)
            {
                WaveInterop.waveInClose(handle);
                handle = IntPtr.Zero;
            }

            ReleaseBuffers();
            disposed = true;
        }

        private void InitializeBuffers(WaveFormat format)
        {
            ReleaseBuffers();

            var bytesPerBuffer = Math.Max(format.AverageBytesPerSecond * BufferMilliseconds / 1000, format.BlockAlign);
            bytesPerBuffer = Align(bytesPerBuffer, format.BlockAlign);

            lock (bufferLock)
            {
                for (var i = 0; i < NumberOfBuffers; i++)
                {
                    var buffer = new WaveInBuffer(handle, bytesPerBuffer);
                    bufferLookup[buffer.HeaderPtr] = buffer;
                    buffer.Queue();
                }
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

        private void ReleaseBuffers()
        {
            lock (bufferLock)
            {
                foreach (var buffer in bufferLookup.Values)
                {
                    buffer.Dispose();
                }

                bufferLookup.Clear();
            }
        }

        private void OnWaveInProc(IntPtr hWaveIn, WaveInMessage message, IntPtr dwInstance, IntPtr headerPtr, IntPtr reserved)
        {
            if (message != WaveInMessage.Data || headerPtr == IntPtr.Zero || disposed)
            {
                return;
            }

            WaveInBuffer buffer;
            lock (bufferLock)
            {
                if (!bufferLookup.TryGetValue(headerPtr, out buffer))
                {
                    return;
                }
            }

            var bytes = buffer.BytesRecorded;
            if (bytes > 0)
            {
                var managedCopy = new byte[bytes];
                Buffer.BlockCopy(buffer.Buffer, 0, managedCopy, 0, bytes);
                DataAvailable?.Invoke(this, new WaveInEventArgs(managedCopy, bytes));
            }

            if (!disposed)
            {
                buffer.Queue();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WaveInEvent));
            }
        }

        private sealed class WaveInBuffer : IDisposable
        {
            private readonly IntPtr waveInHandle;
            private readonly byte[] buffer;
            private readonly GCHandle bufferHandle;
            private readonly IntPtr headerPtr;
            private bool prepared;

            public WaveInBuffer(IntPtr handle, int size)
            {
                waveInHandle = handle;
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
            }

            public byte[] Buffer => buffer;

            public IntPtr HeaderPtr => headerPtr;

            public int BytesRecorded
            {
                get
                {
                    var header = Marshal.PtrToStructure<WaveHeader>(headerPtr);
                    return Math.Max(0, Math.Min(header.dwBytesRecorded, buffer.Length));
                }
            }

            public void Queue()
            {
                if (!prepared)
                {
                    WaveInterop.ThrowOnError(WaveInterop.waveInPrepareHeader(waveInHandle, headerPtr, WaveInterop.HeaderSize), "waveInPrepareHeader");
                    prepared = true;
                }

                WaveInterop.ThrowOnError(WaveInterop.waveInAddBuffer(waveInHandle, headerPtr, WaveInterop.HeaderSize), "waveInAddBuffer");
            }

            public void Dispose()
            {
                if (prepared)
                {
                    WaveInterop.waveInUnprepareHeader(waveInHandle, headerPtr, WaveInterop.HeaderSize);
                    prepared = false;
                }

                Marshal.FreeHGlobal(headerPtr);
                if (bufferHandle.IsAllocated)
                {
                    bufferHandle.Free();
                }
            }
        }
    }
}

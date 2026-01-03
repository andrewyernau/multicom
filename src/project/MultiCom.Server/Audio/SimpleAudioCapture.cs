using System;
using System.Runtime.InteropServices;

namespace MultiCom.Server.Audio
{
    public class SimpleAudioCapture : IDisposable
    {
        private const int WAVE_MAPPER = -1;
        private const int WAVE_FORMAT_PCM = 1;
        private IntPtr waveIn;
        private WaveInProc waveInProc;
        private bool isRecording;
        private GCHandle[] bufferHandles;
        private byte[][] buffers;
        
        public event EventHandler<AudioDataEventArgs> DataAvailable;

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private delegate void WaveInProc(IntPtr hwi, int uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        private static extern int waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX pwfx, WaveInProc dwCallback, IntPtr dwInstance, int fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveInPrepareHeader(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInAddBuffer(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInStart(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInStop(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInReset(IntPtr hwi);

        [DllImport("winmm.dll")]
        private static extern int waveInUnprepareHeader(IntPtr hwi, ref WAVEHDR pwh, int cbwh);

        [DllImport("winmm.dll")]
        private static extern int waveInClose(IntPtr hwi);

        public void StartRecording(int sampleRate = 8000, int bitsPerSample = 16, int channels = 1)
        {
            if (isRecording) return;

            WAVEFORMATEX format = new WAVEFORMATEX
            {
                wFormatTag = WAVE_FORMAT_PCM,
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)bitsPerSample,
                nBlockAlign = (ushort)(channels * bitsPerSample / 8),
                nAvgBytesPerSec = (uint)(sampleRate * channels * bitsPerSample / 8),
                cbSize = 0
            };

            waveInProc = new WaveInProc(WaveInCallback);
            int result = waveInOpen(out waveIn, WAVE_MAPPER, ref format, waveInProc, IntPtr.Zero, 0x00030000);
            
            if (result != 0)
                throw new Exception($"Error opening wave input: {result}");

            // Crear buffers
            int bufferSize = sampleRate * channels * bitsPerSample / 8 / 10; // 100ms
            buffers = new byte[3][];
            bufferHandles = new GCHandle[3];

            for (int i = 0; i < 3; i++)
            {
                buffers[i] = new byte[bufferSize];
                bufferHandles[i] = GCHandle.Alloc(buffers[i], GCHandleType.Pinned);

                WAVEHDR header = new WAVEHDR
                {
                    lpData = bufferHandles[i].AddrOfPinnedObject(),
                    dwBufferLength = (uint)bufferSize,
                    dwBytesRecorded = 0,
                    dwUser = (IntPtr)i,
                    dwFlags = 0,
                    dwLoops = 0
                };

                waveInPrepareHeader(waveIn, ref header, Marshal.SizeOf(header));
                waveInAddBuffer(waveIn, ref header, Marshal.SizeOf(header));
            }

            waveInStart(waveIn);
            isRecording = true;
        }

        private void WaveInCallback(IntPtr hwi, int uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            const int WIM_DATA = 0x3C0;
            
            if (uMsg == WIM_DATA && isRecording)
            {
                WAVEHDR header = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
                int bufferIndex = (int)header.dwUser;
                
                if (header.dwBytesRecorded > 0)
                {
                    byte[] data = new byte[header.dwBytesRecorded];
                    Array.Copy(buffers[bufferIndex], data, header.dwBytesRecorded);
                    DataAvailable?.Invoke(this, new AudioDataEventArgs(data, (int)header.dwBytesRecorded));
                }

                // Re-agregar buffer
                waveInPrepareHeader(waveIn, ref header, Marshal.SizeOf(header));
                waveInAddBuffer(waveIn, ref header, Marshal.SizeOf(header));
            }
        }

        public void Stop()
        {
            if (!isRecording) return;

            isRecording = false;
            waveInStop(waveIn);
            waveInReset(waveIn);

            for (int i = 0; i < bufferHandles.Length; i++)
            {
                if (bufferHandles[i].IsAllocated)
                    bufferHandles[i].Free();
            }

            waveInClose(waveIn);
            waveIn = IntPtr.Zero;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class AudioDataEventArgs : EventArgs
    {
        public byte[] Buffer { get; }
        public int BytesRecorded { get; }

        public AudioDataEventArgs(byte[] buffer, int bytesRecorded)
        {
            Buffer = buffer;
            BytesRecorded = bytesRecorded;
        }
    }
}

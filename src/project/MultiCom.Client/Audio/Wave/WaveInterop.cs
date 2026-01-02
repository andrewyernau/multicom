using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    internal static class WaveInterop
    {
        internal const int MMSYSERR_NOERROR = 0;
        internal const int CALLBACK_FUNCTION = 0x00030000;
        internal const int WAVE_MAPPER = -1;

        private static readonly int waveHeaderSize = Marshal.SizeOf(typeof(WaveHeader));

        internal static int HeaderSize => waveHeaderSize;

        [DllImport("winmm.dll")]
        internal static extern int waveInGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        internal static extern int waveInGetDevCaps(IntPtr hWaveIn, ref WaveInCaps waveInCaps, int sizeOfWaveInCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        internal static extern int waveInOpen(out IntPtr hWaveIn, int deviceId, ref WaveFormatNative waveFormat, WaveInProc callback, IntPtr callbackInstance, int flags);

        [DllImport("winmm.dll")]
        internal static extern int waveInStart(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        internal static extern int waveInStop(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        internal static extern int waveInReset(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        internal static extern int waveInClose(IntPtr hWaveIn);

        [DllImport("winmm.dll")]
        internal static extern int waveInPrepareHeader(IntPtr hWaveIn, IntPtr waveInHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveInUnprepareHeader(IntPtr hWaveIn, IntPtr waveInHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveInAddBuffer(IntPtr hWaveIn, IntPtr waveInHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveOutOpen(out IntPtr hWaveOut, int deviceId, ref WaveFormatNative waveFormat, WaveOutProc callback, IntPtr callbackInstance, int flags);

        [DllImport("winmm.dll")]
        internal static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr waveOutHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr waveOutHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveOutWrite(IntPtr hWaveOut, IntPtr waveOutHeader, int sizeOfWaveHdr);

        [DllImport("winmm.dll")]
        internal static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        internal static extern int waveOutClose(IntPtr hWaveOut);

        internal static void ThrowOnError(int result, string operation)
        {
            if (result != MMSYSERR_NOERROR)
            {
                throw new InvalidOperationException($"[AGENT] {operation} failed with code {result}.");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WaveHeader
    {
        public IntPtr lpData;
        public int dwBufferLength;
        public int dwBytesRecorded;
        public IntPtr dwUser;
        public int dwFlags;
        public int dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WaveInCaps
    {
        public short wMid;
        public short wPid;
        public int vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public int dwFormats;
        public short wChannels;
        public short wReserved1;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WaveOutCaps
    {
        public short wMid;
        public short wPid;
        public int vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public int dwFormats;
        public short wChannels;
        public short wReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WaveFormatNative
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;
    }

    internal delegate void WaveInProc(IntPtr hWaveIn, WaveInMessage message, IntPtr dwInstance, IntPtr waveHeaderPtr, IntPtr reserved);

    internal delegate void WaveOutProc(IntPtr hWaveOut, WaveOutMessage message, IntPtr dwInstance, IntPtr waveHeaderPtr, IntPtr reserved);

    internal enum WaveInMessage : int
    {
        Open = 0x3BE,
        Close = 0x3BF,
        Data = 0x3C0
    }

    internal enum WaveOutMessage : int
    {
        Open = 0x3BB,
        Close = 0x3BC,
        Done = 0x3BD
    }

    [Flags]
    internal enum WaveHeaderFlags
    {
        Done = 0x00000001,
        Prepared = 0x00000002,
        BeginLoop = 0x00000004,
        EndLoop = 0x00000008,
        InQueue = 0x00000010
    }
}

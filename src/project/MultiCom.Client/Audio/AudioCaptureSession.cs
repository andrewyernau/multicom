using System;
using NAudio.Wave;
using MultiCom.Shared.Audio;

namespace MultiCom.Client.Audio
{
    internal sealed class AudioCaptureSession : IDisposable
    {
        private readonly WaveInEvent waveIn;
        private bool disposed;

        public event EventHandler<byte[]> BufferReady;

        public AudioCaptureSession(int deviceNumber)
        {
            var selectedDevice = deviceNumber < 0 ? 0 : deviceNumber;
            waveIn = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                BufferMilliseconds = AudioFormat.BUFFER_MILLISECONDS,
                WaveFormat = new WaveFormat(AudioFormat.SAMPLE_RATE, AudioFormat.BITS_PER_SAMPLE, AudioFormat.CHANNELS)
            };
            waveIn.DataAvailable += OnDataAvailable;
        }

        public void Start()
        {
            ThrowIfDisposed();
            waveIn.StartRecording();
        }

        public void Stop()
        {
            if (disposed)
            {
                return;
            }

            waveIn.StopRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (disposed || e.BytesRecorded <= 0)
            {
                return;
            }

            var handler = BufferReady;
            if (handler == null)
            {
                return;
            }

            var managed = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, managed, 0, managed.Length);
            handler(this, managed);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AudioCaptureSession));
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.Dispose();
        }
    }
}

using System;
using NAudio.Wave;

namespace MultiCom.Client.Audio
{
    public class SimpleAudioPlayer : IDisposable
    {
        private readonly WaveOutEvent waveOut;
        private readonly BufferedWaveProvider waveProvider;

        public SimpleAudioPlayer()
        {
            waveOut = new WaveOutEvent();
            waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            waveProvider.DiscardOnBufferOverflow = true;
            waveOut.Init(waveProvider);
        }

        public void Start()
        {
            waveOut.Play();
        }

        public void AddSamples(byte[] buffer, int offset, int count)
        {
            waveProvider.AddSamples(buffer, offset, count);
        }

        public void Dispose()
        {
            waveOut?.Dispose();
        }
    }
}

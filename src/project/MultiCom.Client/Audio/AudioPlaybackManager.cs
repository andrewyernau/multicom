using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NAudio.Wave;
using MultiCom.Shared.Audio;

namespace MultiCom.Client.Audio
{
    internal sealed class AudioPlaybackManager : IDisposable
    {
        private sealed class PlaybackSession : IDisposable
        {
            private readonly WaveOutEvent output;
            private readonly BufferedWaveProvider buffer;

            public PlaybackSession()
            {
                buffer = new BufferedWaveProvider(new WaveFormat(AudioFormat.SAMPLE_RATE, AudioFormat.BITS_PER_SAMPLE, AudioFormat.CHANNELS))
                {
                    DiscardOnBufferOverflow = true
                };
                output = new WaveOutEvent();
                output.Init(buffer);
                output.Play();
            }

            public void Push(byte[] pcm)
            {
                if (pcm == null || pcm.Length == 0)
                {
                    return;
                }

                buffer.AddSamples(pcm, 0, pcm.Length);
            }

            public void Dispose()
            {
                output?.Stop();
                output?.Dispose();
            }
        }

        private readonly ConcurrentDictionary<Guid, PlaybackSession> sessions = new ConcurrentDictionary<Guid, PlaybackSession>();

        public void PushSamples(Guid senderId, byte[] pcm)
        {
            var session = sessions.GetOrAdd(senderId, _ => new PlaybackSession());
            session.Push(pcm);
        }

        public void Prune(IEnumerable<Guid> activeSenders)
        {
            if (activeSenders == null)
            {
                return;
            }

            var whitelist = new HashSet<Guid>(activeSenders);
            foreach (var pair in sessions)
            {
                if (!whitelist.Contains(pair.Key))
                {
                    if (sessions.TryRemove(pair.Key, out var session))
                    {
                        session.Dispose();
                    }
                }
            }
        }

        public void Clear()
        {
            foreach (var pair in sessions)
            {
                if (sessions.TryRemove(pair.Key, out var session))
                {
                    session.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

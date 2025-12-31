using System;
using System.Collections.Generic;

namespace MultiCom.Shared.Networking
{
    public sealed class PerformanceSnapshot
    {
        private readonly double averageLatencyMs;
        private readonly double jitterMs;
        private readonly double framesPerSecond;
        private readonly int lostPackets;

        public PerformanceSnapshot(double averageLatencyMs, double jitterMs, double framesPerSecond, int lostPackets)
        {
            this.averageLatencyMs = averageLatencyMs;
            this.jitterMs = jitterMs;
            this.framesPerSecond = framesPerSecond;
            this.lostPackets = lostPackets;
        }

        public double AverageLatencyMs { get { return averageLatencyMs; } }
        public double JitterMs { get { return jitterMs; } }
        public double FramesPerSecond { get { return framesPerSecond; } }
        public int LostPackets { get { return lostPackets; } }
    }

    public sealed class PerformanceTracker
    {
        private readonly Queue<double> latencySamples = new Queue<double>();
        private readonly object gate = new object();
        private readonly int maxSamples;
        private readonly TimeSpan fpsWindow = TimeSpan.FromSeconds(1);

        private DateTime lastFpsTimestamp = DateTime.UtcNow;
        private int framesWithinWindow;
        private double currentFps;
        private double? lastLatency;
        private double jitterAccumulator;
        private int jitterSamples;
        private int lostPackets;

        public PerformanceTracker(int maxSamples = 100)
        {
            this.maxSamples = Math.Max(4, maxSamples);
        }

        public void RegisterFrame(DateTime receivedAtUtc, double latencyMs)
        {
            lock (gate)
            {
                latencySamples.Enqueue(latencyMs);
                if (latencySamples.Count > maxSamples)
                {
                    latencySamples.Dequeue();
                }

                framesWithinWindow++;
                if (receivedAtUtc - lastFpsTimestamp >= fpsWindow)
                {
                    currentFps = framesWithinWindow / (receivedAtUtc - lastFpsTimestamp).TotalSeconds;
                    framesWithinWindow = 0;
                    lastFpsTimestamp = receivedAtUtc;
                }

                if (lastLatency.HasValue)
                {
                    jitterAccumulator += Math.Abs(lastLatency.Value - latencyMs);
                    jitterSamples++;
                }

                lastLatency = latencyMs;
            }
        }

        public void RegisterLoss(int lost)
        {
            if (lost <= 0)
            {
                return;
            }

            lock (gate)
            {
                lostPackets += lost;
            }
        }

        public PerformanceSnapshot BuildSnapshot()
        {
            lock (gate)
            {
                var averageLatency = 0d;
                foreach (var sample in latencySamples)
                {
                    averageLatency += sample;
                }

                if (latencySamples.Count > 0)
                {
                    averageLatency /= latencySamples.Count;
                }

                var jitter = jitterSamples > 0 ? jitterAccumulator / jitterSamples : 0d;
                return new PerformanceSnapshot(averageLatency, jitter, currentFps, lostPackets);
            }
        }

        public void Reset()
        {
            lock (gate)
            {
                latencySamples.Clear();
                framesWithinWindow = 0;
                currentFps = 0;
                lastLatency = null;
                jitterAccumulator = 0;
                jitterSamples = 0;
                lostPackets = 0;
                lastFpsTimestamp = DateTime.UtcNow;
            }
        }
    }
}

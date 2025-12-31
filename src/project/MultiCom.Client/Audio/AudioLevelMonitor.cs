using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MultiCom.Client.Audio
{
    internal sealed class AudioLevelMonitor : IDisposable
    {
        private readonly IMMDeviceEnumerator enumerator;
        private readonly IMMDevice device;
        private readonly IAudioMeterInformation meter;
        private readonly Timer timer;
        private bool disposed;

        public event EventHandler<float> LevelAvailable;

        public AudioLevelMonitor(string deviceId = null)
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            IMMDevice captureDevice;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eCommunications, out captureDevice));
            }
            else
            {
                Marshal.ThrowExceptionForHR(enumerator.GetDevice(deviceId, out captureDevice));
            }

            device = captureDevice;

            var iid = typeof(IAudioMeterInformation).GUID;
            object meterInstance;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, CLSCTX.INPROC_SERVER, IntPtr.Zero, out meterInstance));
            meter = (IAudioMeterInformation)meterInstance;

            timer = new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(150));
        }

        private void OnTick(object state)
        {
            if (disposed)
            {
                return;
            }

            try
            {
                float level;
                if (meter.GetPeakValue(out level) == 0)
                {
                    var handler = LevelAvailable;
                    if (handler != null)
                    {
                        handler(this, level);
                    }
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            timer.Dispose();

            if (meter != null)
            {
                Marshal.ReleaseComObject(meter);
            }

            if (device != null)
            {
                Marshal.ReleaseComObject(device);
            }

            if (enumerator != null)
            {
                Marshal.ReleaseComObject(enumerator);
            }
        }
    }
}

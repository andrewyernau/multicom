using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace MultiCom.Client.Audio
{
    internal sealed class AudioDeviceInfo
    {
        public AudioDeviceInfo(string id, string name, int deviceNumber)
        {
            Id = id;
            Name = name;
            DeviceNumber = deviceNumber;
        }

        public string Id { get; }
        public string Name { get; }
        public int DeviceNumber { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class AudioDeviceCatalog
    {
        public static IReadOnlyList<AudioDeviceInfo> EnumerateCaptureDevices()
        {
            var devices = new List<AudioDeviceInfo>();
            try
            {
                for (var device = 0; device < WaveIn.DeviceCount; device++)
                {
                    var caps = WaveIn.GetCapabilities(device);
                    devices.Add(new AudioDeviceInfo(device.ToString(), caps.ProductName, device));
                }
            }
            catch
            {
                return Array.Empty<AudioDeviceInfo>();
            }

            if (devices.Count == 0)
            {
                return Array.Empty<AudioDeviceInfo>();
            }

            devices.Insert(0, new AudioDeviceInfo(string.Empty, "System default", -1));
            return devices;
        }
    }
}

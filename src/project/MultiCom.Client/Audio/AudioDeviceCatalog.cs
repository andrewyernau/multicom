using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MultiCom.Client.Audio
{
    internal sealed class AudioDeviceInfo
    {
        public AudioDeviceInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class AudioDeviceCatalog
    {
        private const uint DEVICE_STATE_ACTIVE = 0x00000001;

        private static readonly PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            pid = 14
        };

        public static IReadOnlyList<AudioDeviceInfo> EnumerateCaptureDevices()
        {
            var devices = new List<AudioDeviceInfo>();
            IMMDeviceEnumerator enumerator = null;
            IMMDeviceCollection collection = null;
            try
            {
                enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(EDataFlow.eCapture, DEVICE_STATE_ACTIVE, out collection));
                uint count;
                collection.GetCount(out count);
                for (uint i = 0; i < count; i++)
                {
                    IMMDevice device;
                    collection.Item(i, out device);
                    string id;
                    device.GetId(out id);
                    var name = TryGetFriendlyName(device);
                    devices.Add(new AudioDeviceInfo(id, string.IsNullOrWhiteSpace(name) ? id : name));
                    Marshal.ReleaseComObject(device);
                }
            }
            catch
            {
            }
            finally
            {
                if (collection != null)
                {
                    Marshal.ReleaseComObject(collection);
                }

                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }

            devices.Insert(0, new AudioDeviceInfo(string.Empty, "System default"));
            return devices;
        }

        private static string TryGetFriendlyName(IMMDevice device)
        {
            IPropertyStore store;
            if (device.OpenPropertyStore((int)STGM.READ, out store) != 0)
            {
                return null;
            }

            try
            {
                PROPVARIANT value;
                var key = PKEY_Device_FriendlyName;
                if (store.GetValue(ref key, out value) != 0)
                {
                    return null;
                }

                var ptr = value.pointerValue;
                var friendlyName = ptr != IntPtr.Zero ? Marshal.PtrToStringUni(ptr) : null;
                PropVariantNative.PropVariantClear(ref value);
                return friendlyName;
            }
            finally
            {
                if (store != null)
                {
                    Marshal.ReleaseComObject(store);
                }
            }
        }
    }
}

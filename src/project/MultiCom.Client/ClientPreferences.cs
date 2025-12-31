using System.Net;

namespace MultiCom.Client
{
    internal sealed class ClientPreferences
    {
        public string DisplayName { get; set; }
        public int CameraIndex { get; set; }
        public string AudioDeviceId { get; set; }
        public IPAddress PreferredInterface { get; set; }
        public IPAddress ServerAddress { get; set; }
        public int CaptureWidth { get; set; }
        public int CaptureHeight { get; set; }
        public int CaptureFps { get; set; }
        public long JpegQuality { get; set; }
    }
}

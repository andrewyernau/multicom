using System;

namespace MultiCom.Shared
{
    public class ParticipantInfo
    {
        public string ClientID { get; set; }
        public string IPAddress { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
        public int FrameCount { get; set; }
        public int AudioPacketCount { get; set; }

        public ParticipantInfo(string clientId, string ipAddress)
        {
            ClientID = clientId;
            IPAddress = ipAddress;
            LastSeen = DateTime.Now;
            IsActive = true;
            FrameCount = 0;
            AudioPacketCount = 0;
        }

        public void UpdateActivity()
        {
            LastSeen = DateTime.Now;
            IsActive = true;
        }

        public bool IsTimedOut(int timeoutSeconds = 30)
        {
            return (DateTime.Now - LastSeen).TotalSeconds > timeoutSeconds;
        }

        public override string ToString()
        {
            return $"{ClientID} ({IPAddress}) - Active: {IsActive}";
        }
    }
}

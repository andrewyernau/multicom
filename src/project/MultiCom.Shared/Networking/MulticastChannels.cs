using System.Net;

namespace MultiCom.Shared.Networking
{
    public static class MulticastChannels
    {
        public const string VIDEO_ADDRESS = "224.0.0.1";
        public const int VIDEO_PORT = 8080;
        public const string CHAT_ADDRESS = "224.0.0.1";
        public const int CHAT_PORT = 8082;
        public const string AUDIO_ADDRESS = "224.0.0.1";
        public const int AUDIO_PORT = 8081;
        public const string CONTROL_ADDRESS = "224.0.0.1";
        public const int CONTROL_PORT = 8083;

        public static IPEndPoint BuildVideoEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse(VIDEO_ADDRESS), VIDEO_PORT);
        }

        public static IPEndPoint BuildChatEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse(CHAT_ADDRESS), CHAT_PORT);
        }

        public static IPEndPoint BuildAudioEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse(AUDIO_ADDRESS), AUDIO_PORT);
        }

        public static IPEndPoint BuildControlEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse(CONTROL_ADDRESS), CONTROL_PORT);
        }
    }
}

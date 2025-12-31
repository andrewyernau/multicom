using System.Net;

namespace MultiCom.Shared.Networking
{
    public static class MulticastChannels
    {
        public const string VIDEO_ADDRESS = "239.50.10.1";
        public const int VIDEO_PORT = 5050;
        public const string CHAT_ADDRESS = "239.50.10.2";
        public const int CHAT_PORT = 5051;
        public const string AUDIO_ADDRESS = "239.50.10.3";
        public const int AUDIO_PORT = 5052;
        public const string CONTROL_ADDRESS = "239.50.10.4";
        public const int CONTROL_PORT = 5053;

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

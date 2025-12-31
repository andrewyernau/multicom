namespace MultiCom.Shared.Audio
{
    public static class AudioFormat
    {
        public const int SAMPLE_RATE = 8000;
        public const int CHANNELS = 1;
        public const int BITS_PER_SAMPLE = 16;
        public const int BUFFER_MILLISECONDS = 50;
        public const int PCM_BYTES_PER_SAMPLE = BITS_PER_SAMPLE / 8;
        public const int BUFFER_SAMPLES = SAMPLE_RATE * BUFFER_MILLISECONDS / 1000;
        public const int PCM_BUFFER_LENGTH = BUFFER_SAMPLES * PCM_BYTES_PER_SAMPLE;
    }
}

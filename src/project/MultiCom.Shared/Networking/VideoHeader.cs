using System;

namespace MultiCom.Shared.Networking
{
    /// <summary>
    /// Cabecera mejorada para transmisión de video
    /// Tamaño total: 32 bytes
    /// </summary>
    public class VideoHeader
    {
        public const int HEADER_SIZE = 32;
        private const uint MAGIC_NUMBER = 0x56494445; // "VIDE" en hexadecimal

        // Campos de la cabecera
        public uint MagicNumber { get; set; }           // 4 bytes - Validación
        public uint ImageNumber { get; set; }           // 4 bytes - Número de secuencia de imagen
        public long TimestampUtc { get; set; }          // 8 bytes - Timestamp NTP-like en ticks
        public int PayloadSize { get; set; }            // 4 bytes - Tamaño del payload
        public ushort Width { get; set; }               // 2 bytes - Ancho de imagen
        public ushort Height { get; set; }              // 2 bytes - Alto de imagen
        public byte Quality { get; set; }               // 1 byte - Calidad JPEG (0-100)
        public byte Flags { get; set; }                 // 1 byte - Flags (reservado para futuro)
        public uint Checksum { get; set; }              // 4 bytes - CRC32 simple del payload
        public ushort Reserved { get; set; }            // 2 bytes - Reservado

        public VideoHeader()
        {
            MagicNumber = MAGIC_NUMBER;
        }

        public byte[] ToBytes()
        {
            byte[] header = new byte[HEADER_SIZE];
            int offset = 0;

            Buffer.BlockCopy(BitConverter.GetBytes(MagicNumber), 0, header, offset, 4);
            offset += 4;

            Buffer.BlockCopy(BitConverter.GetBytes(ImageNumber), 0, header, offset, 4);
            offset += 4;

            Buffer.BlockCopy(BitConverter.GetBytes(TimestampUtc), 0, header, offset, 8);
            offset += 8;

            Buffer.BlockCopy(BitConverter.GetBytes(PayloadSize), 0, header, offset, 4);
            offset += 4;

            Buffer.BlockCopy(BitConverter.GetBytes(Width), 0, header, offset, 2);
            offset += 2;

            Buffer.BlockCopy(BitConverter.GetBytes(Height), 0, header, offset, 2);
            offset += 2;

            header[offset++] = Quality;
            header[offset++] = Flags;

            Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, header, offset, 4);
            offset += 4;

            Buffer.BlockCopy(BitConverter.GetBytes(Reserved), 0, header, offset, 2);

            return header;
        }

        public static VideoHeader FromBytes(byte[] data)
        {
            if (data == null || data.Length < HEADER_SIZE)
                throw new ArgumentException($"Data must be at least {HEADER_SIZE} bytes");

            var header = new VideoHeader();
            int offset = 0;

            header.MagicNumber = BitConverter.ToUInt32(data, offset);
            offset += 4;

            if (header.MagicNumber != MAGIC_NUMBER)
                throw new InvalidOperationException("Invalid magic number - corrupted header");

            header.ImageNumber = BitConverter.ToUInt32(data, offset);
            offset += 4;

            header.TimestampUtc = BitConverter.ToInt64(data, offset);
            offset += 8;

            header.PayloadSize = BitConverter.ToInt32(data, offset);
            offset += 4;

            header.Width = BitConverter.ToUInt16(data, offset);
            offset += 2;

            header.Height = BitConverter.ToUInt16(data, offset);
            offset += 2;

            header.Quality = data[offset++];
            header.Flags = data[offset++];

            header.Checksum = BitConverter.ToUInt32(data, offset);
            offset += 4;

            header.Reserved = BitConverter.ToUInt16(data, offset);

            return header;
        }

        public static byte[] CreatePacket(VideoHeader header, byte[] payload)
        {
            byte[] packet = new byte[HEADER_SIZE + payload.Length];
            byte[] headerBytes = header.ToBytes();

            Buffer.BlockCopy(headerBytes, 0, packet, 0, HEADER_SIZE);
            Buffer.BlockCopy(payload, 0, packet, HEADER_SIZE, payload.Length);

            return packet;
        }

        public static bool TryParsePacket(byte[] packet, out VideoHeader header, out byte[] payload)
        {
            header = null;
            payload = null;

            if (packet == null || packet.Length < HEADER_SIZE)
                return false;

            try
            {
                header = FromBytes(packet);
                payload = new byte[packet.Length - HEADER_SIZE];
                Buffer.BlockCopy(packet, HEADER_SIZE, payload, 0, payload.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calcula un checksum simple CRC32-like del payload
        /// </summary>
        public static uint CalculateChecksum(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            uint checksum = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                checksum ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((checksum & 1) != 0)
                        checksum = (checksum >> 1) ^ 0xEDB88320;
                    else
                        checksum >>= 1;
                }
            }
            return ~checksum;
        }

        public bool VerifyChecksum(byte[] payload)
        {
            return Checksum == CalculateChecksum(payload);
        }
    }
}

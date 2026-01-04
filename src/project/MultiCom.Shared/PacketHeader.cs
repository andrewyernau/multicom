using System;
using System.Text;

namespace MultiCom.Shared
{
    /// <summary>
    /// Cabecera de paquete para video/audio con identificación de cliente
    /// Total: 44 bytes
    /// </summary>
    public class PacketHeader
    {
        public const int HEADER_SIZE = 44;
        public const int CLIENT_ID_SIZE = 16;

        public string ClientID { get; set; }
        public long Timestamp { get; set; }
        public int ImageNumber { get; set; }
        public int SequenceNumber { get; set; }
        public int TotalPackets { get; set; }
        public int TotalSize { get; set; }
        public int ChunkSize { get; set; }

        public byte[] ToBytes()
        {
            byte[] header = new byte[HEADER_SIZE];
            
            // ClientID (16 bytes)
            byte[] clientIdBytes = Encoding.UTF8.GetBytes(ClientID.PadRight(CLIENT_ID_SIZE).Substring(0, CLIENT_ID_SIZE));
            Buffer.BlockCopy(clientIdBytes, 0, header, 0, CLIENT_ID_SIZE);
            
            // Timestamp (8 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(Timestamp), 0, header, 16, 8);
            
            // ImageNumber (4 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(ImageNumber), 0, header, 24, 4);
            
            // SequenceNumber (4 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(SequenceNumber), 0, header, 28, 4);
            
            // TotalPackets (4 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(TotalPackets), 0, header, 32, 4);
            
            // TotalSize (4 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(TotalSize), 0, header, 36, 4);
            
            // ChunkSize (4 bytes)
            Buffer.BlockCopy(BitConverter.GetBytes(ChunkSize), 0, header, 40, 4);
            
            return header;
        }

        public static PacketHeader FromBytes(byte[] data)
        {
            if (data.Length < HEADER_SIZE)
                throw new ArgumentException($"Data must be at least {HEADER_SIZE} bytes");

            var header = new PacketHeader();
            
            // ClientID (16 bytes)
            byte[] clientIdBytes = new byte[CLIENT_ID_SIZE];
            Buffer.BlockCopy(data, 0, clientIdBytes, 0, CLIENT_ID_SIZE);
            header.ClientID = Encoding.UTF8.GetString(clientIdBytes).TrimEnd();
            
            // Timestamp (8 bytes)
            header.Timestamp = BitConverter.ToInt64(data, 16);
            
            // ImageNumber (4 bytes)
            header.ImageNumber = BitConverter.ToInt32(data, 24);
            
            // SequenceNumber (4 bytes)
            header.SequenceNumber = BitConverter.ToInt32(data, 28);
            
            // TotalPackets (4 bytes)
            header.TotalPackets = BitConverter.ToInt32(data, 32);
            
            // TotalSize (4 bytes)
            header.TotalSize = BitConverter.ToInt32(data, 36);
            
            // ChunkSize (4 bytes)
            header.ChunkSize = BitConverter.ToInt32(data, 40);
            
            return header;
        }

        public static byte[] CreatePacket(PacketHeader header, byte[] payload)
        {
            byte[] packet = new byte[HEADER_SIZE + payload.Length];
            byte[] headerBytes = header.ToBytes();
            
            Buffer.BlockCopy(headerBytes, 0, packet, 0, HEADER_SIZE);
            Buffer.BlockCopy(payload, 0, packet, HEADER_SIZE, payload.Length);
            
            return packet;
        }

        public static void ParsePacket(byte[] packet, out PacketHeader header, out byte[] payload)
        {
            if (packet.Length < HEADER_SIZE)
                throw new ArgumentException($"Packet must be at least {HEADER_SIZE} bytes");

            header = FromBytes(packet);
            
            payload = new byte[packet.Length - HEADER_SIZE];
            Buffer.BlockCopy(packet, HEADER_SIZE, payload, 0, payload.Length);
        }
    }
}

using System;

namespace MultiCom.Shared.Networking
{
    /// <summary>
    /// Estadísticas de recepción de video para detectar pérdidas
    /// </summary>
    public class VideoStatistics
    {
        private uint lastImageNumber = 0;
        private bool firstPacket = true;
        private int totalReceived = 0;
        private int totalLost = 0;
        private int outOfOrder = 0;
        private int corrupted = 0;

        public int TotalReceived { get { return totalReceived; } }
        public int TotalLost { get { return totalLost; } }
        public int OutOfOrder { get { return outOfOrder; } }
        public int Corrupted { get { return corrupted; } }
        
        public double LossRate 
        { 
            get 
            { 
                int total = totalReceived + totalLost;
                return total > 0 ? (double)totalLost / total * 100.0 : 0.0;
            } 
        }

        /// <summary>
        /// Registra la recepción de un paquete y detecta pérdidas
        /// </summary>
        /// <returns>Número de paquetes perdidos detectados</returns>
        public int RegisterPacket(uint imageNumber)
        {
            int lostCount = 0;

            if (firstPacket)
            {
                firstPacket = false;
                lastImageNumber = imageNumber;
                totalReceived++;
                return 0;
            }

            // Detectar pérdidas
            uint expectedNext = lastImageNumber + 1;
            
            if (imageNumber == expectedNext)
            {
                // Paquete en orden
                lastImageNumber = imageNumber;
                totalReceived++;
            }
            else if (imageNumber > expectedNext)
            {
                // Paquetes perdidos
                lostCount = (int)(imageNumber - expectedNext);
                totalLost += lostCount;
                lastImageNumber = imageNumber;
                totalReceived++;
            }
            else
            {
                // Paquete fuera de orden o duplicado
                outOfOrder++;
                totalReceived++;
            }

            return lostCount;
        }

        public void RegisterCorrupted()
        {
            corrupted++;
        }

        public void Reset()
        {
            lastImageNumber = 0;
            firstPacket = true;
            totalReceived = 0;
            totalLost = 0;
            outOfOrder = 0;
            corrupted = 0;
        }

        public string GetSummary()
        {
            return string.Format(
                "Recibidos: {0} | Perdidos: {1} | Fuera de orden: {2} | Corruptos: {3} | Tasa pérdida: {4:F2}%",
                totalReceived, totalLost, outOfOrder, corrupted, LossRate
            );
        }
    }
}

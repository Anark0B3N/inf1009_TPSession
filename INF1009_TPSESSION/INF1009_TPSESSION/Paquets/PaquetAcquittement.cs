using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetAcquittement
    {
        private byte[] paquet;

        public PaquetAcquittement (byte numeroConnexion, byte pr)
        {
            paquet = new byte[2];
            paquet[0] = numeroConnexion;
            paquet[1] = (byte)((pr << 5) | 0x01);
        }

        public byte[] Paquet { get => paquet; set => paquet = value; }

        //Getters
        public byte getPR() {
            return paquet[1];
        }

        //Setters
        public void setPR(byte pr) {
            paquet[1] = pr;
        }
    }
}

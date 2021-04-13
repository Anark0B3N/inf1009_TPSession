using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetDemandeLiberation
    {
        private byte[] paquet;

        public PaquetDemandeLiberation(byte numeroConnexion, byte adresseSource, byte adresseDestination)
        {
            paquet = new byte[4];
            paquet[0] = numeroConnexion;
            paquet[1] = 0x13;
            paquet[2] = adresseSource;
            paquet[3] = adresseDestination;
        }
       

        public byte[] Paquet { get => paquet; set => paquet = value; }
    }
}

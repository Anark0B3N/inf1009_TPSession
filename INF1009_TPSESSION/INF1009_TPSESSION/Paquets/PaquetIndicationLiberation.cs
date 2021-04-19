using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetIndicationLiberation : Paquet
    {
        

        public PaquetIndicationLiberation(byte numeroConnexion, byte adresseSource, byte adresseDestination, byte raison)
        {
            paquet = new byte[5];
            paquet[0] = numeroConnexion;
            paquet[1] = 0x13;
            paquet[2] = adresseSource;
            paquet[3] = adresseDestination;
            paquet[4] = raison;
        }

        public byte[] Paquet { get => paquet; set => paquet = value; }
    }
}

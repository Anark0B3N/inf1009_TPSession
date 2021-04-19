using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetAppel : Paquet
    {
        
        

        public PaquetAppel(byte numeroConnexion, byte adresseSource, byte adresseDestination)
        {
            
            paquet = new byte[4];
            paquet[0] = numeroConnexion;
            paquet[1] = Constantes.N_CONNECT_REQ;
            paquet[2] = adresseSource;
            paquet[3] = adresseDestination;
        }

        public byte[] Paquet { get => paquet; set => paquet = value; }
    }
}

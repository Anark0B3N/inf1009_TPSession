using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetDemandeLiberation : Paquet
    {
        

        public PaquetDemandeLiberation(byte numeroConnexion, byte adresseSource, byte adresseDestination)
        {
            paquet = new byte[5];
            paquet[0] = numeroConnexion;
            paquet[1] = Constantes.N_DISCONNECT_REQ;
            paquet[2] = adresseSource;
            paquet[3] = adresseDestination;     
        }
       

        public byte[] Paquet { get => paquet; set => paquet = value; }
    }
}

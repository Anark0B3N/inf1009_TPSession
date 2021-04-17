using System;
using System.Collections.Generic;
using System.Text;

/* Connexions (S_lec / ET)
* byte[0] : etat de la connexion (attente ou connecte)
* byte[1] : adresse source
* byte[2] : adresses destination
*/
namespace INF1009_TPSESSION {

    class ConnexionTransport {
        private byte[] data;

        public ConnexionTransport(byte etat, byte src, byte dest) {
            data = new byte[3];

            data[0] = etat;
            data[1] = etat;
            data[0] = etat;
        }

        public byte getEtat() {
            return data[0];
        }
        public byte getSrc() {
            return data[1];
        }
        public byte getDest() {
            return data[2];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION.Paquets
{
    class Paquet
    {
        protected byte[] paquet;
       
        //Getter
        public byte[] getPaquet(){
            return paquet;
        }
        public byte getNoConn() {
            return paquet[0];
        }
        public byte getType() {
            return paquet[1];
        }
        public byte getSrc() {
            return paquet[2];
        }
        public byte getDest() {
            return paquet[3];
        }

        //Setters
        public void setNoConn(byte noConn) {
            paquet[0] = noConn;
        }
        public void setType(byte type) {
            paquet[1] = type;
        }
        public void setSrc(byte src) {
            paquet[2] = src;
        }
        public void setDest(byte dest) {
            paquet[3] = dest;
        }
    }
}

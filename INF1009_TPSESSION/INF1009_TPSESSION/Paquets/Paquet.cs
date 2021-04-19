using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION.Paquets
{
    class Paquet
    {
        protected byte[] paquet;
        public Paquet()
        {
            ;
        }
       
        public byte[] getPaquet()
        {
            return paquet;
        }
    }
}

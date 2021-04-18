using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    static class Constantes
    {
        public const byte EN_ATTENTE = 0x00;
        public const byte CONNECTE = 0x01;


        public const byte N_CONNECT_REQ = 0x02;
        public const byte N_CONNECT_IND = 0x03;
        public const byte N_CONNECT_RESP = 0x04;
        public const byte N_CONNECT_CONF = 0x05;
        public const byte N_DATA_REQ = 0x06;
        public const byte N_DATA_IND = 0x07;
        public const byte N_DISCONNECT_REQ = 0x08;
        public const byte N_DISCONNECT_IND = 0x09;
    }
}

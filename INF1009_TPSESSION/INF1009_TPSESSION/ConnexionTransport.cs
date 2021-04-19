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
        private Queue<string> commands;

        public ConnexionTransport(byte src, byte dest, byte numeroConnexion) {
            data = new byte[3];
            commands = new Queue<string>();

            data[0] = src;
            data[1] = dest;
            data[2] = numeroConnexion;

        }

        public byte getSrc() {
            return data[0];
        }
        public byte getDest() {
            return data[1];
        }
        public byte getNumeroConnexion()
        {
            return data[2];
        }
        public void addCommand(string command) {
            commands.Enqueue(command);
        }

        public string getNextCommand() {
            if (commands.Count == 0)
                return null;

            return commands.Dequeue();
        }
    }
}

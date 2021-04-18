using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION {
    class EntiteeReseau {
        private ConnexionTransport connexinTransport;

        public EntiteeReseau(ConnexionTransport connexionTransport) {
            this.connexinTransport = connexionTransport;
        }

        public void Execute() {
            string cmd = connexinTransport.getNextCommand();
            while (cmd != null) {
                processTask(cmd);
                cmd = connexinTransport.getNextCommand();
            }
        }

        private void processTask(string task) {
            string[] taskParams = task.Split(',');

            switch (taskParams[0]) {
                case "DebutDesDonnees":
                    //todo: connection
                    break;
                case "FinDesDonnees":
                    //todo: deconnexion
                    break;
                case "DesDonnees":
                    //faire des paquets
                    break;


            }
        }
    }
}

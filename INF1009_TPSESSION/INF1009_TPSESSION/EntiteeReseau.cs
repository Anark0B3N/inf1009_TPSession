using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace INF1009_TPSESSION {
    class EntiteeReseau {
        private ConnexionTransport connexionTransport;
        public static Semaphore L_lec_sem;
        public static Semaphore L_ecr_sem;

        public EntiteeReseau(ConnexionTransport connexionTransport) {
            this.connexionTransport = connexionTransport;
            L_lec_sem = new Semaphore(1,1);
            L_ecr_sem = new Semaphore(1,1);
        }

        public void Execute() {
            string cmd = connexionTransport.getNextCommand();
            while (cmd != null) {
                processTask(cmd);
                cmd = connexionTransport.getNextCommand();
            }
        }

        private void processTask(string task) {
            //string[] taskParams = task.Split(',');
            Paquet paquetRetour;
            switch (task) {
                case "DebutDesDonnees":
                    paquetRetour = liaisonDonnees(new PaquetAppel(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest()));          
                    break;
                case "FinDesDonnees":
                    //todo: deconnexion
                    break;
                    //Data transfer
                default:
                    if (task.Length <= 128)
                        paquetRetour = liaisonDonnees(new PaquetDonnees(connexionTransport.getNumeroConnexion(), 0x22, Encoding.ASCII.GetBytes(task)));
                    else
                    {
                        //TODO: 
                    }
                    //faire des paquets
                    break;


            }
        }
        private Paquet liaisonDonnees(Paquet paquetRecu)
        {
            switch (paquetRecu.getPaquet()[Constantes.TYPE_PAQUET])
            {
                case Constantes.N_CONNECT_REQ:
                    byte? connexion =  establishConnexion(paquetRecu);
                    //Si le distant à refusé la connexion
                    if (connexion == Constantes.N_DISCONNECT_IND)
                        return new PaquetIndicationLiberation(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION], 0x01);
                    //Si le distant a accepté la connexion 
                    else if (connexion == Constantes.N_CONNECT_IND)
                        return new PaquetConnexionEtablie(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION]);
                    //Si le distant ne répond pas
                    else if(connexion == null)
                    {
                        return new PaquetIndicationLiberation(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION], 0x01);
                    }                    
                    break;

                    //Pour une demande de déconnexion 
                case Constantes.N_DISCONNECT_REQ:
                    break;

                    //Data pack with less than 120 Bytes of data
                default:
                    //Si il n'y a qu'une seule trame de data
                    if ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x10) == 0)
                    {
                        //byte? 
                    }
                       
                    break;
            }
            return null;
        }

        private byte? establishConnexion(Paquet paquetRecu)
        {
            writeData("tentative de connexion Source: " + ((int)paquetRecu.getPaquet()[2]).ToString() + ", Destination: " + ((int)paquetRecu.getPaquet()[3]).ToString() + " " + DateTime.Now);
            //Pas de réponse
            if ((int)paquetRecu.getPaquet()[2] % 19 == 0)
            {
                writeLog("...");
                return null;
            }
            //Connexion refusée
            else if ((int)paquetRecu.getPaquet()[2] % 13 == 0)
            {
                writeLog("Connexion refusée " + DateTime.Now);
                return Constantes.N_DISCONNECT_IND;
            }
            //Connexion établie
            else
            {
                writeLog("Connexion établie " + DateTime.Now);
                return Constantes.N_CONNECT_IND;
            }
        }

        private void writeLog(string log)
        {
            L_lec_sem.WaitOne();
            System.IO.StreamWriter writer = new StreamWriter(@"L_lec.txt", true);           
            writer.WriteLine(log);              
            writer.Close();           
            L_lec_sem.Release();
        }

        private void writeData(string log)
        {
            
            L_ecr_sem.WaitOne();
            System.IO.StreamWriter writer = new StreamWriter(@"L_ecr.txt", true);           
            writer.WriteLine(log);
            writer.Close();           
            L_ecr_sem.Release();
        }
    }
}

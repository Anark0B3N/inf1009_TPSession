using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace INF1009_TPSESSION {
    class EntiteeReseau {
        private ConnexionTransport connexionTransport;
        private string trameComplete;
        public static Semaphore L_lec_sem;
        public static Semaphore L_ecr_sem;
        int etat;

        public EntiteeReseau(ConnexionTransport connexionTransport) {
            this.connexionTransport = connexionTransport;
            L_lec_sem = new Semaphore(1,1);
            L_ecr_sem = new Semaphore(1,1);
        }

        public void Execute() {
            etat = Constantes.EN_ATTENTE;
            trameComplete = "";
            string cmd = connexionTransport.getNextCommand();
            while (cmd != null) {
                if (etat == Constantes.DECONNECTE)
                    break;
                processTask(cmd);
                cmd = connexionTransport.getNextCommand();
            }
        }

        private void processTask(string task) {
            Paquet paquetRetour;

            switch (task) {
                case "DebutDesDonnees":

                    //Si l'Adresse source est un multiple de 27, la couche réseau refuse la connexion
                    if (connexionTransport.getSrc() % 27 == 0)
                    {
                        paquetRetour = new PaquetIndicationLiberation(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest(), 0x02);
                        etat = Constantes.DECONNECTE;
                    }
                    else
                    {
                        paquetRetour = liaisonDonnees(new PaquetAppel(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest()));
                    }
                    if (paquetRetour != null)
                        returnData(paquetRetour);
                    break;                    
                //Demande de deconnexion
                case "FinDesDonnees":
                    paquetRetour = liaisonDonnees(new PaquetDemandeLiberation(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest()));
                    if (paquetRetour != null)
                        returnData(paquetRetour);
                    etat = Constantes.DECONNECTE;

                    break;

                //Data transfer
                default:

                    if (task.Length <= 128) {
                        byte[] taskBytes = Encoding.ASCII.GetBytes(task);
                        int triesCount = 0;
                        paquetRetour = tryWithAcquittementNegatif(liaisonDonnees, new PaquetDonnees(connexionTransport.getNumeroConnexion(), 0x00, taskBytes), ref triesCount);
                        if((paquetRetour.getType() & 0x09) == 0x09) {
                            //2x aqcuittement negatif -> deconnexion
                            etat = Constantes.DECONNECTE;
                            return;
                        }
                        else if (paquetRetour != null && paquetRetour.getPaquet().Length > 2)
                            returnData(paquetRetour);
                    }
                    else {
                        int noPaquet = 0;
                        for (int i = 0; i < task.Length; i += 128) {
                            Paquet paquetTemporaire;

                            if ((task.Length - i) >= 128) {
                                byte type = (byte)((noPaquet << 5) | 0x10 | ((noPaquet + 1) << 1));
                                byte[] taskBytes = Encoding.ASCII.GetBytes(task.Substring(i, 128));

                                paquetTemporaire = new PaquetDonnees(connexionTransport.getNumeroConnexion(), type, taskBytes);
                            }
                            //Dernier paquet de donnes
                            else {
                                byte type = (byte)((noPaquet << 5) | (noPaquet << 1));
                                byte[] taskBytes = Encoding.ASCII.GetBytes(task.Substring(i, task.Length % 128));

                                paquetTemporaire = new PaquetDonnees(connexionTransport.getNumeroConnexion(), type, taskBytes);
                            }

                            noPaquet++;

                            int triescount = 0;
                            paquetRetour = tryWithAcquittementNegatif(liaisonDonnees, paquetTemporaire, ref triescount);

                            if ((paquetRetour.getType() & 0x09) == 0x09) {
                                //2x aqcuittement negatif -> deconnexion
                                etat = Constantes.DECONNECTE;
                                return;
                            }

                        }
                    }
                    //faire des paquets
                    break;
            }
        }

        private void returnData (Paquet paquetRetour)
        {
            string paquetString = "";
            foreach(byte b in paquetRetour.getPaquet()){
                paquetString += (Convert.ToString(b) + ",");
            }
            Transport.ERs_TO_ET_File.WaitOne();
            File.AppendAllText("R_ecr.txt", paquetString + "\n");
            Transport.ERs_TO_ET_File.Release();
        }

        private Paquet liaisonDonnees(Paquet paquetRecu)
        {
            int triesCount;

            switch (paquetRecu.getType())
            {
                case Constantes.N_CONNECT_REQ:
                    triesCount = 0;
                    byte? connexion = tryWithTemp(establishConnexion, paquetRecu, ref triesCount);

                    //Si le distant à refusé la connexion
                    if (connexion == Constantes.N_DISCONNECT_IND) {
                        etat = Constantes.DECONNECTE;
                        writeData("paquet d'indication liberation");
                        return new PaquetIndicationLiberation(paquetRecu.getNoConn(), paquetRecu.getSrc(), paquetRecu.getDest(), 0x01);
                    }

                    //Si le distant a accepté la connexion 
                    else if (connexion == Constantes.N_CONNECT_IND) {
                        etat = Constantes.CONNECTE;
                        writeData("paquet de connexion etablie");
                        return new PaquetConnexionEtablie(paquetRecu.getNoConn(), paquetRecu.getSrc(), paquetRecu.getDest());
                    }

                    //Si le distant ne répond pas (connexion = null quand trop long, voir tryWithTemp)
                    else if (connexion == null) {
                        etat = Constantes.DECONNECTE;
                        writeData("paquet d'indication liberation");
                        return new PaquetIndicationLiberation(paquetRecu.getNoConn(), paquetRecu.getSrc(), paquetRecu.getDest(), 0x01);
                    }                    
                    break;

                //Pour une demande de déconnexion 
                case Constantes.N_DISCONNECT_REQ:
                    writeLog("Demande de déconnexion Source: " + paquetRecu.getSrc() + "\t\t" + DateTime.Now);
                    writeData("paquet d'indication liberation");
                    return new PaquetIndicationLiberation(paquetRecu.getNoConn(), paquetRecu.getSrc(), paquetRecu.getDest(), 0x01);
                    break;

                //Data
                default:
                    byte? retourData = receiveData(paquetRecu);

                    if((retourData & 0x09) == 0x09) {
                        //Aquittement negatif
                        writeData("paquet d'aqcuittement negatif");
                        return new PaquetAcquittementNegatif(paquetRecu.getNoConn(), (byte)(retourData >> 5));
                    }else if((retourData & 0x09) == 0x01) {
                        //Acquittement positif
                        writeData("paquet d'aqcuittement positif");
                        return new PaquetAcquittement(paquetRecu.getNoConn(), (byte)(retourData >> 5));
                    }

                    break;
            }
            return null;
        }


        /*Permet d'essayer une methode du type 'byte? XYZ(Paquet)' avec un temporisateur avec 
         * un nombre d'essaies max de 2. Retourne null si le nombre d'essai max est depasse*/
        private byte? tryWithTemp(Func<Paquet, byte?> funcToTry, Paquet paquetRecu, ref int triesCount) {
            long time0 = DateTime.Now.Ticks; //ticks / 10 000 000 = seconds

            //Appel de la function a tester
            byte? result = funcToTry(paquetRecu);

            long temporisateurMax = 15000000;//1.5 secs

            //Si le temporisateur est ecoule
            if ((DateTime.Now.Ticks - time0) > temporisateurMax) {
                triesCount++;
                if (triesCount > 2)
                    return null;

                //Re-essayer
                return tryWithTemp(funcToTry, paquetRecu, ref triesCount);
            }
            else {
                return result;
            }
        }

        /*Permet d'essayer une methode du type 'Paquet XYZ(Paquet)' avec un 
         * nombre d'essaies max de 2. Retourne null si le nombre d'essai max est depasse*/
        private Paquet tryWithAcquittementNegatif(Func<Paquet, Paquet> funcToTry, Paquet paquetRecu, ref int triesCount) {
            //Appel de la function a tester
            Paquet result = funcToTry(paquetRecu);

            if((result.getType() & 0x09) == 0x09) {
                triesCount++;
                if (triesCount > 2)
                    return null;

                //Re-essayer
                return tryWithAcquittementNegatif(funcToTry, paquetRecu, ref triesCount);
            }
            else {
                return result;
            }
        }

        private byte? establishConnexion(Paquet paquetRecu)
        {
            //writeData("tentative de connexion Source: " + ((int)paquetRecu.getSrc()).ToString() + ", Destination: " + ((int)paquetRecu.getDest()).ToString() + "\t\t" + DateTime.Now);
            //Pas de réponse
            if ((int)paquetRecu.getSrc() % 19 == 0)
            {
                writeLog(".....simulation de non reponse.....");

                Thread.Sleep(1500); //simulation de non-reponse
                return null;
            }
            //Connexion refusée
            else if ((int)paquetRecu.getSrc() % 13 == 0)
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

        private byte? receiveData(Paquet paquetRecu)
        {
            Random rnd = new Random();
            //Ne retourne rien si l'adresse source est un multiple de 15
            if ((paquetRecu.getSrc() % 15) == 0) {
                writeLog(".....simulation de non reponse.....");

                Thread.Sleep(1500); //simulation de non-reponse
                return null;
            }
            //Acquitement négatif
            else if ((int)((paquetRecu.getType() >> 1) & 0x07) == rnd.Next(0, 7)) {
                writeLog("paquet de données invalide reçu de " + paquetRecu.getSrc() + "\t\t" + DateTime.Now);             
                return ((byte?)((paquetRecu.getType() & 0xE0) | 0x09));
            }

            //Acquitement Positif
            else {
                //S'il n'y a qu'une seule trame / dernier paquet
                if ((paquetRecu.getType() & 0x10) == 0) {
                    trameComplete += (Encoding.ASCII.GetString(paquetRecu.getPaquet(), 2, 128));
                    //Fin de la trame
                    writeData(trameComplete);
                    trameComplete = "";
                
                    writeLog("Paquet de données reçu #" + (paquetRecu.getType() >> 5) + ". Prochain: #" + ((paquetRecu.getType() & 0x0E) >> 1) + ". source: " + paquetRecu.getSrc() + "\t\t" + DateTime.Now);
                    return ((byte?)((paquetRecu.getType() & 0xE0) | 0x01));
                }

                //Sinon paquet d'une suite
                trameComplete += (Encoding.ASCII.GetString(paquetRecu.getPaquet(), 2, 128));
                writeLog("Paquet de données reçu #" + (paquetRecu.getType() >> 5) + ". Prochain: #" + ((paquetRecu.getType() & 0x0E) >> 1) + ". source: " + paquetRecu.getSrc() + "\t\t" + DateTime.Now);
                return (byte?)((paquetRecu.getType() & (0x0E << 4)) | 0x01);
            }
                
        }

       

        private void writeLog(string log)
        {
            L_lec_sem.WaitOne();
            File.AppendAllText("L_lec.txt", connexionTransport.getNumeroConnexion().ToString() + ": " + log + "\n");
            L_lec_sem.Release();
        }

        private void writeData(string log)
        {
            
            L_ecr_sem.WaitOne();
            File.AppendAllText("L_ecr.txt", connexionTransport.getNumeroConnexion().ToString() + ": " + log + "\n");   
            L_ecr_sem.Release();
        }
    }

}

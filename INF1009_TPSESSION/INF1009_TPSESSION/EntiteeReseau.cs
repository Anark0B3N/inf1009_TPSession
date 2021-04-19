﻿using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace INF1009_TPSESSION {
    class EntiteeReseau {
        private ConnexionTransport connexionTransport;
        private string trameComplete;
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

        //
        //TODO return paquet to ET
        //
        private void processTask(string task) {
            //string[] taskParams = task.Split(',');
            Paquet paquetRetour;
            Paquet paquetTemporaire;

            switch (task) {
                case "DebutDesDonnees":

                    //Si l'Adresse source est un multiple de 27, la couche réseau refuse la connexion
                    if (connexionTransport.getSrc() % 27 == 0)
                    {
                        paquetRetour = new PaquetIndicationLiberation(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest(), 0x02);
                    }
                    else
                    {
                        paquetRetour = liaisonDonnees(new PaquetAppel(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest()));
                    }
                        break;
                    //Demande de connexion
                case "FinDesDonnees":
                    paquetRetour = liaisonDonnees(new PaquetDemandeLiberation(connexionTransport.getNumeroConnexion(), connexionTransport.getSrc(), connexionTransport.getDest()));
                    break;

                    //Data transfer
                default:
                    if (task.Length <= 128)
                        paquetRetour = liaisonDonnees(new PaquetDonnees(connexionTransport.getNumeroConnexion(), 0x10, Encoding.ASCII.GetBytes(task)));
                    else
                    {

                        for(int i = 0; i < task.Length; i += 128)
                        {
                            if ( (task.Length - i) >= 128 )
                            {
                                byte type = (byte)((i << 5) | 0x10 | ((i + 1) << 1));
                                paquetTemporaire = new PaquetDonnees(connexionTransport.getNumeroConnexion(), type, Encoding.ASCII.GetBytes(task.Substring(i, 128)));
                            }
                            else
                            {
                                byte type = (byte)((i << 5) | 0x10 | (i << 1));
                                paquetTemporaire = new PaquetDonnees(connexionTransport.getNumeroConnexion(), type, Encoding.ASCII.GetBytes(task.Substring(i, task.Length % 128 )));
                            }
                                paquetRetour = liaisonDonnees(paquetTemporaire);
                        }
                    }
                    //faire des paquets
                    break;


            }
        }
        private Paquet liaisonDonnees(Paquet paquetRecu)
        {
            int triesCount;

            switch (paquetRecu.getPaquet()[Constantes.TYPE_PAQUET])
            {
                case Constantes.N_CONNECT_REQ:
                    triesCount = 0;
                    byte? connexion = tryWithTemp(establishConnexion, paquetRecu, ref triesCount);

                    //Si le distant à refusé la connexion
                    if (connexion == Constantes.N_DISCONNECT_IND)
                        return new PaquetIndicationLiberation(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION], 0x01);
                    //Si le distant a accepté la connexion 
                    else if (connexion == Constantes.N_CONNECT_IND)
                        return new PaquetConnexionEtablie(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION]);
                    //Si le distant ne répond pas
                    //TODO ++++ Devrait être un temporisateur ++++++ 
                    else if(connexion == null)
                    {
                        return new PaquetIndicationLiberation(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION], 0x01);
                    }                    
                    break;

                    //Pour une demande de déconnexion 
                case Constantes.N_DISCONNECT_REQ:
                    writeLog("Demande de déconnexion Source: " + paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] + " " + DateTime.Now);
                    return new PaquetIndicationLiberation(paquetRecu.getPaquet()[Constantes.NUMERO_CONNEXION], paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE], paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION], 0x01);
                    break;

                default:
                    triesCount = 0;
                    byte? retourData = tryWithTemp(receiveData, paquetRecu, ref triesCount);
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

        private byte? establishConnexion(Paquet paquetRecu)
        {
            writeData("tentative de connexion Source: " + ((int)paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE]).ToString() + ", Destination: " + ((int)paquetRecu.getPaquet()[Constantes.ADRESSE_DESTINATION]).ToString() + " " + DateTime.Now);
            //Pas de réponse
            if ((int)paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] % 19 == 0)
            {
                writeLog("...");

                Thread.Sleep(1500); //simulation de non-reponse
                return null;
            }
            //Connexion refusée
            else if ((int)paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] % 13 == 0)
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
            if ((paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] % 15) == 0) {
                writeLog("...");

                Thread.Sleep(1500); //simulation de non-reponse
                return null;
            }
            //Acquitement négatif
            else if ((int)((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] >> 1) & 0x07) == rnd.Next(0, 7)) {
                writeLog("paquet de données invalide reçu de " + paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] + " " + DateTime.Now);
                //
                // Check this, not sure why I need to cast it as byte
                //
                return ((byte?)((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0xF0) | 0x09));
            }

            //Acquitement Positif
            else {
                //S'il n'y a qu'une seule trame
                if ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x10) == 0) {
                    writeLog("Paquet de données reçu 1 de 1. source: " + paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] + " " + DateTime.Now);
                    writeData(Encoding.UTF8.GetString(paquetRecu.getPaquet(), 2, 128));
                    return ((byte?)((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0xE0) | 0x01));
                }
                else {
                    //Premier paquet d'une suite
                    if (((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0xE0) | 0x00) == 0) {
                        trameComplete = "";
                        trameComplete += (Encoding.UTF8.GetString(paquetRecu.getPaquet(), 2, 128));
                        writeLog("Paquet de données reçu " + ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x0E) >> 5) + " de plusieurs. Source: " + paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] + " " + DateTime.Now);
                    }

                    //Dernier paquet d'une suite
                    if ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0xE0) == (paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x0E) << 4) {
                        writeLog("Paquet de données reçu " + ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x0E) >> 5) + " de " + ((paquetRecu.getPaquet()[Constantes.TYPE_PAQUET] & 0x0E) >> 1) + ". Source: " + paquetRecu.getPaquet()[Constantes.ADRESSE_SOURCE] + " " + DateTime.Now);
                        writeData(trameComplete);
                    }
                }
            }

            return 0;

                
        }

       

        private void writeLog(string log)
        {
            L_lec_sem.WaitOne();
            System.IO.StreamWriter writer = new StreamWriter("L_lec.txt", true);           
            writer.WriteLine(connexionTransport.getNumeroConnexion().ToString() + ": " + log);
            writer.Close();           
            L_lec_sem.Release();
        }

        private void writeData(string log)
        {
            
            L_ecr_sem.WaitOne();
            System.IO.StreamWriter writer = new StreamWriter("L_ecr.txt", true);           
            writer.WriteLine(connexionTransport.getNumeroConnexion().ToString() + ": " + log);
            writer.Close();           
            L_ecr_sem.Release();
        }
    }

    //Segmentation plusieurs paquet
    //retourner paquet et primitives a transport
    //no connexion dans les log
    //temporisateur
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Pipes;
using System.IO;

namespace INF1009_TPSESSION {
    class Transport {

        public static Semaphore ERs_TO_ET_File;
        public static SemaphoreSlim allERsFinished;
        /*public static Semaphore L_lec_sem;
        public static Semaphore L_ecr_sem;*/

        //Liste des threads demarres
        private List<Thread> startedThreads;
        //Table des connexion identifier par le byte identificateur du processus
        private Dictionary<byte, ConnexionTransport> tableControleTransport;

        public Transport() {
            //inits
            ERs_TO_ET_File = new Semaphore(1, 1);
            allERsFinished = new SemaphoreSlim(1);
            /*L_lec_sem = new Semaphore(0, 255);
            L_ecr_sem = new Semaphore(0, 255);*/
            tableControleTransport = new Dictionary<byte, ConnexionTransport>();
            startedThreads = new List<Thread>();

            if (createFiles()) {
                //Lire les lignes et les associes a une connexion
                lectureFichier();

                //Demarre un thread ER par connexion
                startERs();
            }

            //Demarre un thread de lecture et ecriture de reponse (ERs a ET)
            Thread reponsesThread = new Thread(lireReception);
            reponsesThread.Start();

            foreach(Thread th in startedThreads) {
                th.Join();
            }

            //Semaphore utiliser pour notifier le thread de lecture de la fin de la simulation
            allERsFinished.Wait();
            allERsFinished.Release();

            reponsesThread.Join();
        }


        //Lance dans une thread, une entite reseau
        private void startERs() {
            /* Chaque entite possede sa file de tache afin
            *  de ne pas bloquer ET lors d'une attente de 
            *  confirmation d'une connexion */
            foreach (ConnexionTransport conn in tableControleTransport.Values) {
                EntiteeReseau ER = new EntiteeReseau(conn);
                Thread th = new Thread( new ThreadStart(() => ER.Execute()) );
                th.Start();
                startedThreads.Add(th);
            }
        }

        //Création des fichiers s'ils n'existent pas
        public bool createFiles() {
            bool allFileCreated = true;

            //S_lec: source des demandes des apps
            if (!File.Exists("S_lec.txt")) {
                try {
                    using (FileStream fs = File.Create("S_lec.txt")) ;
                }
                catch (Exception e) {
                    allFileCreated = false;
                    Console.WriteLine("can't create S_lec", e.ToString());
                }
            }

            //S_ecr: ecrire dans ce fichier, les retours parvenu a ET
            if (!File.Exists("S_ecr.txt")) {
                try {
                    using (FileStream fs = File.Create("S_ecr.txt")) ;
                }
                catch (Exception e) {
                    allFileCreated = false;
                    Console.WriteLine("Can't ceate S_ecr", e.ToString());
                }

            }
            else
            {
                System.IO.File.WriteAllText(@"S_ecr.txt", string.Empty);
            }

            //R_ecr: ER ecrit dans ce fichier les reponses de liaison pour que ET les recoient
            if (!File.Exists("R_ecr.txt")) {
                try {
                    using (FileStream fs = File.Create("R_ecr.txt")) ;
                }
                catch (Exception e) {
                    allFileCreated = false;
                    Console.WriteLine("Can't ceate R_ecr", e.ToString());
                }

            }

            //L_lec: copies des messages que la couche de liaison de donnees envoie a ER
            if (!File.Exists("L_lec.txt")) {
                try {
                    using (FileStream fs = File.Create("L_lec.txt")) ;
                }
                catch (Exception e) {
                    allFileCreated = false;
                    Console.WriteLine("can't create L_lec", e.ToString());
                }
            }
            else
            {                 
                    System.IO.File.WriteAllText(@"L_lec.txt", string.Empty);
            }

            //L_ecr: ER ecrit dans ce fichier ce qu'il envoie a la couche liaison de donnees
            if (!File.Exists("L_ecr.txt")) {
                try {
                    using (FileStream fs = File.Create("L_ecr.txt")) ;
                }
                catch (Exception e) {
                    allFileCreated = false;
                    Console.WriteLine("Can't ceate L_ecr", e.ToString());
                }

            }
            else
            {
                System.IO.File.WriteAllText(@"L_ecr.txt", string.Empty);
            }

            return allFileCreated;
        }

        //À renomer
        public void lectureFichier() {

            List<string> lignes = new List<string>();
            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(@"S_lec.txt");
            while ((line = file.ReadLine()) != null) {
                lignes.Add(line);
            }

            file.Close();
            foreach (string s in lignes) {
                processLigne(s);
            }
        }


        //Creer une connexion au besoin et ajoute la ligne dans la file de la connexion
        private void processLigne(string ligne) {

            /* Format d'une ligne:
             * no_processus:type_de_pqt,data
             */


            ConnexionTransport currentConn;
            Random rnd = new Random();
            string[] ligneSepare = ligne.Split(':');


            //Recherche d'une connexion existante venant de cette application
            byte processId = Convert.ToByte(ligneSepare[0]);

            if (tableControleTransport.ContainsKey(processId)) {
                //deja connecte
                currentConn = tableControleTransport[processId];
            }
            else {
                int dest = 0, src = 0;
                //Dest et Src doivent etre diffrent..
                while(dest == src) {
                    dest = rnd.Next(1, 255);
                    src = rnd.Next(1, 255);
                }

                //Nouvelle connexion
                currentConn = new ConnexionTransport(Convert.ToByte(src), Convert.ToByte(dest), processId);
                tableControleTransport.Add(processId, currentConn);
            }

            //Ajout de la task dans la file du processus
            currentConn.addCommand(ligneSepare[1]);


            /*currentData = new byte[ligneSepare[1].Length];

            //Récupération de la donnée à transmettre sous forme de table de bytes
            for (int i = 0; i < ligneSepare[1].Length; i++)
            {
                currentData[i] = Convert.ToByte(ligneSepare[1][i]);
            }*/

            Console.WriteLine("Lecture de S_lec: " + ligne);
        }

        //Ecoute par intermittence le fichier R_ecr, soit les reponses de ER, et les ecrient dans S_ecr
        private void lireReception() {
            allERsFinished.Wait();

            int currentLine = 0;
            

            bool run = true;
            while (run) {
                //Mettre un semaphore pour la concurrence du fichier
                ERs_TO_ET_File.WaitOne();
                string[] lines = File.ReadAllLines("R_ecr.txt");
                ERs_TO_ET_File.Release();

                if (File.Exists("R_ecr.txt") && File.Exists("S_ecr.txt")) { //Verifier s'ils existe bien... TODO: debugger ici si rien n'est ecit dans S_ecr
                    for (int i = currentLine; currentLine < lines.Length; currentLine++) {
                        File.AppendAllText("S_ecr.txt", lines[currentLine] + "\n");
                    }
                }


                //Si le semaphore a ete reserve 2 fois, arreter ce thread. La simulation est fini
                if (allERsFinished.CurrentCount < 1) {
                    run = false;
                    Console.WriteLine("ET a finit de lire et d'ecrire les retours");
                }

                //Attendre 1.5 sec avant de re-verifier le fichier txt
                Thread.Sleep(1500);
            }
            allERsFinished.Release();

        }
    }
}

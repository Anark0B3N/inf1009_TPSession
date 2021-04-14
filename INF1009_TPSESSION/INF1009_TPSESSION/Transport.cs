using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace INF1009_TPSESSION
{
    class Transport
    {
        private List<byte[]> tableControleTransport;
        public Transport()
        {
            //Création des fichiers s'ils n'existent pas
            if (!File.Exists("S_lec.txt"))
            {
                try
                {
                    using (FileStream fs = File.Create("S_lec.txt")) ;
                }
                catch (Exception e)
                {
                    Console.WriteLine("can't create S_lec", e.ToString());
                }
            }

            if (!File.Exists("S_ecr.txt"))
            {
                try
                {
                    using (FileStream fs = File.Create("S_ecr.txt")) ;
                }
               
                catch (Exception e)
                {
                    Console.WriteLine("Can't ceate S_ecr", e.ToString());
                }
            
            }
            tableControleTransport = new List<byte[]>();
        }

        //À renomer
        public void lectureFichier()
        {
            
            List<string> lignes = new List<string>();
            string line;           

            System.IO.StreamReader file = new System.IO.StreamReader(@"S_lec.txt");
            while ((line = file.ReadLine()) != null)
            {
                lignes.Add(line);
            }

            file.Close();
            foreach(string s in lignes)
            {
                processLigne(s);
            }
            ;
        }

        private void processLigne(string ligne)
        {
            byte[] currentProcess = new byte[4];
            byte[] currentData;
            string[] ligneSepare = new string[2];
            bool nouvelleConnexion = true;
            Random rnd = new Random();
            ligneSepare = ligne.Split(',');

            //Recherche d'une connexion existante venant de cette application
            foreach (byte[] b in tableControleTransport)
            {
                if (b[0] == Convert.ToByte(ligneSepare[0]))
                {
                    currentProcess = b;
                    nouvelleConnexion = false;
                    break;
                }                  
            }

            //Ajout de la connexion dans la table de cotrôle si elle n'existe pas déja
            if(nouvelleConnexion)
            {
                tableControleTransport.Add(new byte[4] { Convert.ToByte(ligneSepare[0]), Constantes.EN_ATTENTE, Convert.ToByte(rnd.Next(255)), Convert.ToByte(rnd.Next(1, 255)) } );
            }
            currentData = new byte[ligneSepare[1].Length];

            //Récupération de la donnée à transmettre sous forme de table de bytes
            for (int i = 0; i < ligneSepare[1].Length; i++)
            {
                currentData[i] = Convert.ToByte(ligneSepare[1][i]);
            }
        }
    }
}

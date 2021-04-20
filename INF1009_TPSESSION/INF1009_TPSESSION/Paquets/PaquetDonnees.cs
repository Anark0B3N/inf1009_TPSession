using INF1009_TPSESSION.Paquets;
using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetDonnees : Paquet
    {
        

        public PaquetDonnees(byte numeroConnexion, byte typePaquet, byte[] donnees)
        {
            Paquet = new byte[130];
            Paquet[0] = numeroConnexion;
            Paquet[1] = typePaquet;
            for (int i = 0; i < donnees.Length; i++)
            {
                Paquet[i + 2] = donnees[i];
            }

            //Mettre des zéros sur la partie de la trame de data qui n'est pas utilisée
            if (donnees.Length < 128)
            {
                for (int j = donnees.Length + 2; j < 130; j++)
                {
                    Paquet[j] = 0;
                }
            }
        }

        public byte[] Paquet { get => paquet; set => paquet = value; }

        //Getters
        public byte[] getData() {
            byte[] toReturn = new byte[128];
            Array.Copy(paquet, 2, toReturn, 0, 128);
            return toReturn;
        }

        //Setters
        public void setData(byte[] donnees) {
            if (donnees.Length <= 128) {
                Array.Copy(donnees, 0, paquet, 2, donnees.Length);

                //Mettre des zéros sur la partie de la trame de data qui n'est pas utilisée
                if (donnees.Length < 128) {
                    for (int j = donnees.Length + 2; j < 130; j++) {
                        Paquet[j] = 0;
                    }
                }
            }
        }
    }
}

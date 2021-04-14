using System;
using System.Collections.Generic;
using System.Text;

namespace INF1009_TPSESSION
{
    class PaquetDonnees
    {
        private byte[] paquet;

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
                    Paquet[j - 1] = 0;
                }
            }
        }

        public byte[] Paquet { get => paquet; set => paquet = value; }
    }
}

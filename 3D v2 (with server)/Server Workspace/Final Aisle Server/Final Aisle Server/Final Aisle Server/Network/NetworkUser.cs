
using Final_Aisle_Server.Data;
using System;
using System.Security.Cryptography;
/**
* Object that represents a single client connected to the server
*/
namespace Final_Aisle_Server.Network
{
    public class NetworkUser
    {
        public static int IDSequence = 0; // static variable that accumulates to assign a new ID to each new connection

        public int ID { get; set; } // unique among all past/present connections to this server instance
        public int PacketNumber = 0;

        public Connection Connection { get; set; } // represents the connection that connects the client

        public SymmetricAlgorithm UserAES { get; }
        public bool AESKeySent = false;
        public bool SecureConnectionEstablished = false;
        public bool LoggedIn = false; // turn to true when the player is logged in

        public UserAccount UserAccount;

        public NetworkUser()
        {
            Connection = new Connection();
            ID = IDSequence++; // accumulate ID sequence by 1

            UserAES = new RijndaelManaged();
            UserAES.Key = GenerateAES();
            UserAES.Padding = PaddingMode.PKCS7;
        }

        private static byte[] GenerateAES()
        {
            byte[] key = new byte[32];
            Random rand = new Random();
            rand.NextBytes(key);

            return key;
        }
    }
}

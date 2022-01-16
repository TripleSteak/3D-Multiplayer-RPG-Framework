using FinalAisle_Server.LocalData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.Networking
{
    public class NetworkUser
    {
        public int Id { get; set; }
        public static int _idSequence = 0;
        public int PacketNumber = 0;
        public Connection Connection { get; set; }

        public SymmetricAlgorithm UserAES { get; }
        public bool AESKeySent = false;
        public bool SecureConnectionEstablished = false;

        public UserAccount UserAccount;

        public NetworkUser()
        {
            Connection = new Connection();
            Id = _idSequence++;

            UserAES = new RijndaelManaged();
            UserAES.Key = GenerateAES();
            UserAES.Padding = PaddingMode.PKCS7;
        }

        public static byte[] GenerateAES()
        {
            byte[] key = new byte[32];
            Random rand = new Random();
            rand.NextBytes(key);

            return key;
        }
    }
}

using FinalAisle_Shared.Networking.Packet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.LocalData
{
    public static class FileUtils
    {
        public static string ParentDirectory;
        public static string AccountsDirectory;

        public static SymmetricAlgorithm storageAES = new RijndaelManaged(); // storage key

        static FileUtils()
        {
            storageAES.Key = new byte[32] { 167, 227, 157, 141, 7, 84, 186, 226, 163, 164, 22, 52, 90, 199, 77, 88, 145, 91, 100, 46, 36, 0, 51, 19, 254, 130, 6, 217, 102, 40, 179, 105 };
            storageAES.Padding = PaddingMode.PKCS7;

            ParentDirectory = CombineAndCreate(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Server Data");
            AccountsDirectory = CombineAndCreate(ParentDirectory, "Account Data");

            ConsoleLog.WriteDataGeneral("Storage directories loaded.");
        }

        private static void WriteToFile(string directory, string fileName, byte[] data)
        {
            if (data == null || data.Length == 0) return; // empty file calls will NOT override existing data

            string path = CombineAndCreateText(directory, fileName);
            File.WriteAllText(path, String.Empty);

            using (FileStream fs = File.Create(CombineAndCreateText(directory, fileName))) fs.Write(data, 0, data.Length);
        }

        public static void WriteToFile(string directory, string fileName, string data) => WriteToFile(directory, fileName, Encrypt(data));

        private static byte[] ReadBytesFromFile(string directory, string fileName)
        {
            try
            {
                string fullPath = CombineAndCreateText(directory, fileName);
                return File.ReadAllBytes(fullPath);
            }
            catch (Exception) { return new byte[] { }; }
        }

        public static string ReadStringFromFile(string directory, string fileName)
        {
            try
            {
                return Decrypt(ReadBytesFromFile(directory, fileName));
            } catch (Exception)
            {
                return "";
            }
        }

        public static string CombineAndCreate(string parent, string fileName)
        {
            string newPath = Path.Combine(parent, fileName);
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            return newPath;
        }

        public static string CombineAndCreateText(string parent, string fileName)
        {
            fileName = fileName + ".txt";
            string newPath = Path.Combine(parent, fileName);
            if (!File.Exists(newPath)) File.Create(newPath);
            return newPath;
        }

        private static byte[] Encrypt(string message)
        {
            Packet packet = new Packet { Type = PacketType.Message, Data = new MessagePacketData(message) };
            return packet.Serialize(storageAES);
        }

        private static string Decrypt(byte[] data)
        {
            return ((MessagePacketData)Packet.Deserialize(data, storageAES).Data).Message;
        }
    }
}

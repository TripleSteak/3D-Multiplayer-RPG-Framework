using Final_Aisle_Server.Network;
using Final_Aisle_Shared.Network.Packet;
using System;
using System.IO;
using System.Security.Cryptography;

/**
 * Utility class that assists with file manipulation
 */
namespace Final_Aisle_Server.Data
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

            ConsoleLog.WriteSmallData("Storage directories loaded.");
        }

        private static void WriteToFile(string directory, string fileName, byte[] data)
        {
            if (data == null || data.Length == 0) return; // empty file calls will NOT override existing data

            string path = CombineAndCreateText(directory, fileName);
            File.WriteAllText(path, String.Empty);

            using (FileStream fs = File.Create(CombineAndCreateText(directory, fileName))) fs.Write(data, 0, data.Length);
        }

        public static void WriteToFile(string directory, string fileName, string data) => WriteToFile(directory, fileName, StorageEncrypt(data));

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
                return StorageDecrypt(ReadBytesFromFile(directory, fileName));
            }
            catch (Exception)
            {
                return "";
            }
        }

        /**
         * Creates a new folder using the given directory and file name
         */
        public static string CombineAndCreate(string parent, string fileName)
        {
            string newPath = Path.Combine(parent, fileName);
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            return newPath;
        }

        /**
         * Creates a new text file using the given directory and file name
         */
        public static string CombineAndCreateText(string parent, string fileName)
        {
            fileName = fileName + ".txt";
            string newPath = Path.Combine(parent, fileName);
            if (!File.Exists(newPath))
            {
                var stream = File.Create(newPath);
                stream.Close();
            }
            return newPath;
        }

        private static byte[] StorageEncrypt(string message)
        {
            Packet packet = new Packet(PacketType.String, new EmptyPacketData(message)); // storage simply uses empty packet with data as key

            return packet.Serialize(storageAES);
        }

        private static string StorageDecrypt(byte[] data)
        {
            return PacketSerializer.Deserialize(data, storageAES).GetKey();
        }
    }
}

using Final_Aisle_Shared.Network;
using Final_Aisle_Shared.Network.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/**
 * Server-side utility class used for serializing and deserializing JSONs using the server-side version of the Newtonsoft dependency
 */
namespace Final_Aisle_Server.Network
{
    public static class PacketSerializer
    {
        public static Packet Deserialize(IEnumerable<byte> data, SymmetricAlgorithm aes)
        {
            //Console.WriteLine("Deserializing packet -> supposed length: " + BitConverter.ToInt32(data.Take(sizeof(Int32)).ToArray(), 0) + ", actual length: " + (data.Count() - sizeof(Int32)));

            var byteData = data.Skip(sizeof(Int32)).AESDecrypt(aes).Decompress().ToArray(); // skip the first int, which is byte length
            var content = Encoding.ASCII.GetString(byteData);

            Packet packet = JsonConvert.DeserializeObject<Packet>(content, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            return packet;
        }

        public static byte[] Serialize(this Packet packet, SymmetricAlgorithm aes)
        {
            string data = JsonConvert.SerializeObject(packet, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            byte[] byteData = Encoding.ASCII.GetBytes(data).Compress().AESEncrypt(aes).PrependLength().ToArray();
            return byteData;
        }
    }
}

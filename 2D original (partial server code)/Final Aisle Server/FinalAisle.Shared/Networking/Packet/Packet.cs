using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FinalAisle_Shared.Networking.Packet
{
    [Serializable]
    public class Packet
    {
        public PacketType Type { get; set; }
        public PacketData Data { get; set; }

        public static Packet Deserialize(IEnumerable<byte> data, SymmetricAlgorithm aes)
        {
            var byteData = data.Skip(sizeof(int)).AESDecrypt(aes).Decompress().ToArray();
            var content = Encoding.ASCII.GetString(byteData);

            Packet packet = JsonConvert.DeserializeObject<Packet>(content, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            return packet;
        }

        public byte[] Serialize(SymmetricAlgorithm aes)
        {
            string data = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            byte[] byteData = Encoding.ASCII.GetBytes(data).Compress().AESEncrypt(aes).PrependLength().ToArray();

            return byteData;
        }
    }
}

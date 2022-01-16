using System.Collections.Generic;
using System.Net.Sockets;

/**
 *  Collection of variables used to facilitate the connection of a single network user
 */
namespace Final_Aisle_Server.Network
{
    public class Connection
    {
        public Socket Socket { get; set; } // 1 socket per connection
        public const int BufferSize = 1024; // maximum message size per packet
        public byte[] Buffer = new byte[BufferSize]; // buffers incoming messages
        public List<byte> Message = new List<byte>(); // compiled message in bytes
    }
}

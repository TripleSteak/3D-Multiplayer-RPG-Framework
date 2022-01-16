using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.Networking
{
    public class Connection
    {
        public Socket Socket { get; set; }
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public List<byte> Message = new List<byte>();
    }
}

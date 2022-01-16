using FinalAisle_Shared.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.Networking
{
    public class PacketEventArgs
    {
        public NetworkUser User { get; set; }

        public Packet Packet { get; set; }
    }
}

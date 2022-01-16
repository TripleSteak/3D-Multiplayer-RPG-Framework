using Final_Aisle_Shared.Network.Packet;

/**
 * Event object related to packet/user interaction
 */
namespace Final_Aisle_Server.Network.EventArgs
{
    public class PacketEventArgs
    {
        public NetworkUser User { get; set; }
        public Packet Packet { get; set; }
    }
}

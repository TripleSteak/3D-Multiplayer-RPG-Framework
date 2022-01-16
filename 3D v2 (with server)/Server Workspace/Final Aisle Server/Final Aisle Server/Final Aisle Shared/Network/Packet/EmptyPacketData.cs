/**
 * Type of packet that only contains a key
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class EmptyPacketData : PacketData
    {
        public EmptyPacketData(string key) : base(key) { }
    }
}

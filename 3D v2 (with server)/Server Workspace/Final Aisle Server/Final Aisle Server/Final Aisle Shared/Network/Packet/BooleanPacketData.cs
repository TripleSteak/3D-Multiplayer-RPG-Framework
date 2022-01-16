/**
 * Type of packet that interacts with booleans
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class BooleanPacketData : PacketData
    {
        internal bool Value { get; set; }
        public BooleanPacketData(string key, bool value) : base(key) => Value = value;
    }
}

/**
 * Type of packet that interacts with integers
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class IntegerPacketData : PacketData
    {
        internal int Value { get; set; }
        public IntegerPacketData(string key, int value) : base(key) => Value = value;
    }
}

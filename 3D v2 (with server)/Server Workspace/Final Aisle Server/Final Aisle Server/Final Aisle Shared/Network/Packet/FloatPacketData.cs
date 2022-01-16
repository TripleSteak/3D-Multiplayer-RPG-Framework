/**
 * Type of packet that interacts with floats
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class FloatPacketData : PacketData
    {
        internal float Value { get; set; }
        public FloatPacketData(string key, float value) : base(key) => Value = value;
    }
}

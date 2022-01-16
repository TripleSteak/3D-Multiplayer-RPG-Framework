/**
 * Type of packet that interacts with enums
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class EnumPacketData : PacketData
    {
        internal string Value { get; set; } // string representation of enum
        public EnumPacketData(string key, object value) : base(key) => Value = value.ToString();
    }
}

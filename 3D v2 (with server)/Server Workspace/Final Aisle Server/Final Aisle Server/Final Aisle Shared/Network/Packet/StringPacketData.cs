/**
 * Type of packet that interacts with strings
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public class StringPacketData : PacketData
    {
        internal string Value { get; set; }
        public StringPacketData(string key, string value) : base(key) => Value = value;
    }
}

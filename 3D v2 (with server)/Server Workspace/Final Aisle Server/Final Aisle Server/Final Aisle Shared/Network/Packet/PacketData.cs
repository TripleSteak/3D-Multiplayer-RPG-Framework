/**
 * Abstract class for all types of packets' data
 */
namespace Final_Aisle_Shared.Network.Packet
{
    public abstract class PacketData
    {
        public string Key { get; set; } // identifier of the data (aka... what is in this packet?)

        protected PacketData(string key) => Key = key;
    }
}

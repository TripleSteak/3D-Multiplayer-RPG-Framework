using Final_Aisle_Shared.Network.Packet;
using System.Collections.Generic;

/**
* Interface to be inherited by all object classes that require delivery across the network
*/
namespace Final_Aisle_Shared.Network
{
    public interface IPacketSerializable
    {
        List<object> GetSerializableComponents(); // return a list of all objects that are to serialized into a packet
        object Deserialize(CompositePacketData data, int startIndex); // return object containing properties deserialized from compound packet, given starting index
    }
}

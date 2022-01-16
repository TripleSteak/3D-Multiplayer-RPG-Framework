using System;

/**
 * Object class for packets, which are bundles of data sent over the web
 * Contains retrieval methods for reading packets
 * 
 * Note: serialize and deserialize methods have been moved out of the shared folder, as the client/server require different Newtonsoft.Json dependencies
 */
namespace Final_Aisle_Shared.Network.Packet
{
    [Serializable]
    public class Packet
    {
        // packet properties, access through packet methods
        private PacketType Type { get; set; }
        private PacketData Data { get; set; }

        public Packet(PacketType type, PacketData data)
        {
            Type = type;
            Data = data;
        }

        public string GetKey() => Data.Key;

        public bool GetBool()
        {
            if (Type == PacketType.Boolean) return ((BooleanPacketData)Data).Value;
            return false;
        }

        public double GetDouble()
        {
            if (Type == PacketType.Double) return ((DoublePacketData)Data).Value;
            return 0;
        }

        public object GetEnum(Type enumType)
        {
            if (Type == PacketType.Enum) return Enum.Parse(enumType, ((EnumPacketData)Data).Value);
            return null;
        }

        public float GetFloat()
        {
            if (Type == PacketType.Float) return ((FloatPacketData)Data).Value;
            return 0;
        }

        public int GetInt()
        {
            if (Type == PacketType.Integer) return ((IntegerPacketData)Data).Value;
            return 0;
        }

        public string GetString()
        {
            if (Type == PacketType.String) return ((StringPacketData)Data).Value;
            return "";
        }

        /**
         * Allows public access to compound packet methods
         */
        public CompositePacketData GetComposite()
        {
            if (Type == PacketType.Composite) return (CompositePacketData)Data;
            return null;
        }
    }
}

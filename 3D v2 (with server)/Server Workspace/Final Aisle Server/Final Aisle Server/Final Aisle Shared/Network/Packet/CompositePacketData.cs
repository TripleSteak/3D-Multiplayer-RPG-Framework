
using System;
using System.Collections.Generic;
/**
* Type of packet that interacts with composite data (multiple types)
*/
namespace Final_Aisle_Shared.Network.Packet
{
    public class CompositePacketData : PacketData
    {
        internal string Value { get; set; } // compressed string representation of the list
        private string[] expandedList = null; // decompressed version of "Value" variable, only used when reading

        public CompositePacketData(string key, List<object> values) : base(key)
        {
            string[] valueArray = new string[values.Count]; // convert all objects from list parameter to a string
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].GetType().IsPrimitive || values[i].GetType().IsEnum) valueArray[i] = values[i].ToString();
                else throw new Exception("Only primitive data types and enums can be inputted into a composite packets!");
            }

            AbridgeStrings(valueArray); // compress all parameter objects into one string
        }

        /**
         * Returns a boolean value from the composite packet, at the given index
         * 
         * Only call if it is confirmed that the value at the index is a boolean
         */
        public bool GetBool(int index)
        {
            if (expandedList == null) ExpandStringArray();
            return bool.Parse(expandedList[index]);
        }

        /**
         * Returns a double value from the composite packet, at the given index
         * 
         * Only call if it is confirmed that the value at the index is a double
         */
        public double GetDouble(int index)
        {
            if (expandedList == null) ExpandStringArray();
            return double.Parse(expandedList[index]);
        }

        /**
         * Returns an enum value from the composite packet, at the given index
         * 
         * Only call if it is confirmed that the value at the index is an enum
         */
        public object GetEnum(int index, Type enumType)
        {
            if (expandedList == null) ExpandStringArray();
            return Enum.Parse(enumType, expandedList[index]);
        }

        /**
         * Returns a float value from the composite packet, at the given index
         * 
         * Only call if it is confirmed that the value at the index is a float
         */
        public float GetFloat(int index)
        {
            if (expandedList == null) ExpandStringArray();
            return float.Parse(expandedList[index]);
        }

        /**
         * Returns an integer value from the composite packet, at the given index
         * 
         * Only call if it is confirmed that the value at the index is an integer
         */
        public int GetInt(int index)
        {
            if (expandedList == null) ExpandStringArray();
            return int.Parse(expandedList[index]);
        }

        /**
         * Returns a string value from the composite packet, at the given index
         */
        public string GetString(int index)
        {
            if (expandedList == null) ExpandStringArray();
            return expandedList[index];
        }

        /**
         * Condenses an array of multiple strings into a single transmittable string
         * 
         * Value format: (# of messages):(length of message #1):(length of message #2): ... (message 1)(message 2) ...
         * 
         * ALWAYS use AbridgeStrings() when multiple messages must be concatenated into a single packet
         */
        private void AbridgeStrings(string[] messages)
        {
            Value = messages.Length.ToString() + ":";
            for (int i = 0; i < messages.Length; i++) Value += messages[i].Length + ":";
            for (int i = 0; i < messages.Length; i++) Value += messages[i];
        }

        /**
         * Expands an Value string into its contained messages
         * 
         * See #AbridgeStrings()
         */
        private void ExpandStringArray()
        {
            string abridged = Value;
            int length = int.Parse(abridged.Substring(0, abridged.IndexOf(':')));
            abridged = abridged.Substring(abridged.IndexOf(':') + 1);

            string[] messages = new string[length];
            int[] messageLengths = new int[length];

            for (int i = 0; i < length; i++)
            { // retrieve message lengths
                messageLengths[i] = int.Parse(abridged.Substring(0, abridged.IndexOf(':')));
                abridged = abridged.Substring(abridged.IndexOf(':') + 1);
            }

            for (int i = 0; i < length; i++)
            { // retrieve messages
                messages[i] = abridged.Substring(0, messageLengths[i]);
                abridged = abridged.Substring(messageLengths[i]);
            }

            expandedList = messages;
        }
    }
}

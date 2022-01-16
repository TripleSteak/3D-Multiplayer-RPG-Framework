
using Final_Aisle_Shared.Network;
using Final_Aisle_Shared.Network.Packet;
using System;
using System.Collections.Generic;

/**
* Object representation of a player's character; many characters can be bouund to a single account
* 
* Note that serialization only covers fundamental properties, does not include equipment, inventory, etc.
*	- The information conveyed in the serialization is ideally that which is needed to display the character visually
*/
namespace Final_Aisle_Shared.Game.Player
{
	public class PlayerCharacter : IPacketSerializable
	{
		public string AccountUUID { get; set; } // unique ID of the account to which this character belongs
		public string CharacterName { get; set; } // display name of the character itself

		/*
		 * Fundamental character aspects, cannot be modified
		 */
		public PlayerClass Class { get; set; }
		public PlayerRace Race { get; set; }

		/*
		 * Character experience stats
		 */
		public int CharacterLevel { get; set; } // player's "main" level, from questing, fighting, etc.
		public int CharacterExp { get; set; } // progress towards levelling player's character level

		/*
		 * Character combat stats
		 */
		public double MaxHealth { get; set; }
		public double MaxResource { get; set; }

		/**
		 * Empty constructor, call if intending to use Deserialize(string s) to establish properties
		 */
		public PlayerCharacter() { }

		/**
		 * Constructor used to create a new, empty character
		 */
		public PlayerCharacter(string accountUUID, string characterName, PlayerClass classType, PlayerRace race)
		{
			this.AccountUUID = accountUUID;
			this.CharacterName = characterName;
			this.Class = classType;
			this.Race = race;

			this.CharacterLevel = 1; // starting level
			this.CharacterExp = 0;

			// temporary HP/resource numbers, modify later
			this.MaxHealth = 10;
			this.MaxResource = 10;
		}

		List<object> IPacketSerializable.GetSerializableComponents() => new List<object> { AccountUUID, CharacterName, Class, Race };

        object IPacketSerializable.Deserialize(CompositePacketData data, int startIndex)
        {
			this.AccountUUID = data.GetString(startIndex);
			this.CharacterName = data.GetString(startIndex + 1);
			this.Class = (PlayerClass)data.GetEnum(startIndex + 2, typeof(PlayerClass));
			this.Race = (PlayerRace)data.GetEnum(startIndex + 3, typeof(PlayerRace));

			return this;
        }
    }

}

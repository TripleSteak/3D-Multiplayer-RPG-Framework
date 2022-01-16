using Final_Aisle_Shared.Game.Player;
using System;
using System.Collections.Generic;

/**
* Object that corresponds to the user's gameplay account, as opposed to the network user
*/
namespace Final_Aisle_Server.Data
{
    public class UserAccount
    {
        /*
         * AccountUUID is a unique 128-bit one-time generation for each account
         * 
         * Email and password are not stored locally, but rather re-retrieved from storage when necessary
         */
        public string AccountUUID { get; }
        public string Username { get; }

        /*
         * Data regarding character slots
         */
        public int UsedCharacterSlots { set; get; }
        public int TotalCharacterSlots { set; get; }

        public List<PlayerCharacter> CharacterList; // list containing all of the player's characters, list is sorted by main level + exp
        public int ActiveCharacterIndex { set; get; } // the index of the character with which the user is currently playing

        public UserAccount(string accountUUID, string username)
        {
            this.AccountUUID = accountUUID;
            this.Username = username;

            CharacterList = new List<PlayerCharacter>();
            CharacterList.Add(new PlayerCharacter(accountUUID, username + "'s character", PlayerClass.Blank, PlayerRace.Turtle)); // temporary new character creation

            UsedCharacterSlots = CharacterList.Count;
            ActiveCharacterIndex = 0;
        }

        /**
         * Returns the player's currently active character
         */
        public PlayerCharacter GetActiveCharacter()
        {
            try
            {
                return CharacterList[ActiveCharacterIndex];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static UserAccount NewAccount(string email, string username, string password)
        {
            UserAccount account = new UserAccount(AccountDataHandler.GenerateNewAccountUUID(), username);

            AccountDataHandler.AddToEmailUsernameLists(email, username, account.AccountUUID);
            AccountDataHandler.SaveUserData(account, email, username, password);

            return account;
        }

        public static UserAccount LoadAccount(string accountUUID)
        {
            string username = AccountDataHandler.GetUsername(accountUUID);
            UserAccount account = new UserAccount(accountUUID, username);

            return account;
        }
    }
}

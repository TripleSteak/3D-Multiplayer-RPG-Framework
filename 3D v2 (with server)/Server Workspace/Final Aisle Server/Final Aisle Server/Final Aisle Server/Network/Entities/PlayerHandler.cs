using Final_Aisle_Server.Data;
using Final_Aisle_Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/**
 * Static class used for relaying information about player movements/actions to relevant clients
 */
namespace Final_Aisle_Server.Network.Entities
{
    public static class PlayerHandler
    {
        public static List<NetworkUser> OnlineUsers = new List<NetworkUser>(); // in-game, that is to say a character is selected
        public static void MovementInput(NetworkUser user, float moveX, float moveY) => SendToOthers(user, PacketDataUtils.MovementInput, new List<object> { user.ID, moveX, moveY });

        public static void MovementRoll(NetworkUser user, float rotation) => SendToOthers(user, PacketDataUtils.MovementRoll, new List<object> { user.ID, rotation });

        public static void MovementJump(NetworkUser user) => SendToOthers(user, PacketDataUtils.MovementJump, user.ID);

        /**
         * proneState assists in verifying that the player isn't toggling to the wrong state (e.g. already prone)
         */
        public static void MovementToggleProne(NetworkUser user, bool proneState) => SendToOthers(user, PacketDataUtils.MovementToggleProne, new List<object> { user.ID, proneState });

        public static void TransformPosition(NetworkUser user, float posX, float posY, float posZ)
        {
            /*
             * Perform player position verification here, for security purposes:
             * - Can the player have gotten to this position so quickly from the previous position?
             * - Can the player legally be here (not stuck in the ground, for example)?
             */

            SendToOthers(user, PacketDataUtils.TransformPosition, new List<object> { user.ID, posX, posY, posZ });
        }

        public static void TransformRotation(NetworkUser user, float rotation) => SendToOthers(user, PacketDataUtils.TransformRotation, new List<object> { user.ID, rotation });

        /**
         * Officially logs in the player (with an active character), adding them to the list of online players
         */
        public static void LogIn(NetworkUser user)
        {
            ConsoleLog.WriteBigIO(user.UserAccount.Username + " has successfully logged in from " + user.Connection.Socket.RemoteEndPoint);
            user.LoggedIn = true;

            OnlineUsers.Add(user);
            SendToOthers(user, PacketDataUtils.PlayerConnected, (List<object>)new List<object> { user.ID }.Concat(((IPacketSerializable)user.UserAccount.GetActiveCharacter()).GetSerializableComponents())); // tell existing players that a new player joined
        }

        /**
         * Call this method after the player has successfully logged in to (client-side) register all already-registered players into the new player's game
         * (so that when a new player joins, they can actually see players that are already online)
         */
        public static void PostLogIn(NetworkUser user)
        {
            foreach (NetworkUser u in OnlineUsers)
                if (u != user) // don't send the player's data to themselves
                    Program.SendComposite(user, PacketDataUtils.PlayerConnected, (List<object>)new List<object> { u.ID.ToString() }.Concat(((IPacketSerializable)u.UserAccount.GetActiveCharacter()).GetSerializableComponents())); // tell new player of all existing players
        }

        /**
         * If the given user is logged in, will log the player out
         */
        public static void LogOut(NetworkUser user)
        {
            user.LoggedIn = false;
            if (PlayerHandler.OnlineUsers.Contains(user))
            {
                PlayerHandler.OnlineUsers.Remove(user);

                ConsoleLog.WriteBigIO(user.UserAccount.Username + " has logged out.");
            }

            // inform all existing players that a player has left
            foreach (NetworkUser u in OnlineUsers) Program.SendInt(u, PacketDataUtils.PlayerDisconnected, user.ID);
        }

        /**
         * Sends the given message to all players that are not the given user sender
         */
        private static void SendToOthers(NetworkUser sender, string key, object message)
        {
            foreach (NetworkUser u in OnlineUsers)
            {
                if (u.ID != sender.ID) // not the same user, don't send to self!
                {
                    if (message is bool boolean) Program.SendBool(u, key, boolean);
                    else if (message is List<object> list) Program.SendComposite(u, key, list);
                    else if (message is double @double) Program.SendDouble(u, key, @double);
                    else if (message is float @float) Program.SendFloat(u, key, @float);
                    else if (message is int @int) Program.SendInt(u, key, @int);
                    else if (message is string @string) Program.SendString(u, key, @string);
                    else if (message is object obj) Program.SendEnum(u, key, obj);
                }
            }
        }
    }
}

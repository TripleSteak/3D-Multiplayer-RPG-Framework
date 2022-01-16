using FinalAisle_Server.LocalData;
using FinalAisle_Server.Networking.EventArgs;
using FinalAisle_Shared.Networking;
using FinalAisle_Shared.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.Networking
{
    public static class PacketProcessor
    {
        public static void ParseInput(PacketEventArgs args)
        {
            if (args.Packet.Data is MessagePacketData)
            {
                NetworkUser user = args.User;

                string prefix = PacketDataUtils.GetPrefix(((MessagePacketData)args.Packet.Data).Message);
                string details = PacketDataUtils.GetStringData(((MessagePacketData)args.Packet.Data).Message);

                if (user.SecureConnectionEstablished)
                { // data can be safely transported
                    if (prefix.Equals(PacketDataUtils.TryNewAccount))
                    {
                        string email = details.Substring(0, details.IndexOf(' '));
                        details = details.Substring(details.IndexOf(' ') + 1);
                        string username = details.Substring(0, details.IndexOf(' '));
                        string password = details.Substring(details.IndexOf(' ') + 1);

                        ConsoleLog.WriteIOGeneral("User from " + user.Connection.Socket.RemoteEndPoint + " would like to create an account with the following details:");
                        ConsoleLog.WriteIOGeneral("\t   Email: " + email);
                        ConsoleLog.WriteIOGeneral("\tUsername: " + username);
                        ConsoleLog.WriteIOGeneral("\tPassword: " + new String('*', password.Length));

                        if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromEmail(email))) Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.EmailAlreadyTaken, ""));
                        else if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromUsername(username))) Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.UsernameAlreadyTaken, ""));
                        else
                        {
                            Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.EmailVerifySent, ""));

                            string verifyCode = "";
                            if (AccountDataHandler.RegisterData.ContainsKey(user)) verifyCode = AccountDataHandler.RegisterData[user].Item4;
                            if (String.IsNullOrEmpty(verifyCode)) verifyCode = AccountDataHandler.GenerateVerificationCode();

                            AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(email, username, password, verifyCode, AccountDataHandler.TotalVerifyEmailTries); // store temporary login information

                            // Attempt to send verification email to listed email address
                            EmailHandler.SendVerificationMail(email, username, verifyCode);
                        }

                    }
                    else if (prefix.Equals(PacketDataUtils.TryVerifyEmail))
                    {
                        if (!AccountDataHandler.RegisterData.ContainsKey(user) || !details.Equals(AccountDataHandler.RegisterData[user].Item4, StringComparison.InvariantCultureIgnoreCase))
                        { // verification attempt failed
                            ConsoleLog.WriteIOGeneral("User at email " + AccountDataHandler.RegisterData[user].Item1 + " failed verification with attempted code " + details);
                            AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3, AccountDataHandler.RegisterData[user].Item4, AccountDataHandler.RegisterData[user].Item5 - 1);
                            if (AccountDataHandler.RegisterData[user].Item5 <= 0) // out of tries
                            {
                                AccountDataHandler.RegisterData.Remove(user);
                                Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.EmailVerifyFail, "0"));
                            } else Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.EmailVerifyFail, AccountDataHandler.RegisterData[user].Item5.ToString()));
                        } else
                        { // verification attempt succeeded
                            ConsoleLog.WriteIOGeneral("User at email " + AccountDataHandler.RegisterData[user].Item1 + " succeeded in email verification with code " + details);
                            Program.SendStringData(user, PacketDataUtils.Condense(PacketDataUtils.EmailVerifySuccess, ""));

                            // save new account credentials
                            AccountDataHandler.CreateNewAccount(user, AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3);
                            AccountDataHandler.RegisterData.Remove(user);
                        }
                    }

                    /** // GAME DATA
                    if (prefix.Equals(DataHandler.JoinLevel)) // a player has joined a new level
                    {
                        foreach (NetworkUser player in Program.Players)
                        {
                            Program.SendStringData(player, DataHandler.Condense(DataHandler.JoinLevel, ""));
                        }
                    }
                    else if (prefix.Equals(DataHandler.MovementInput)) // player movement input
                    {
                        foreach (NetworkUser player in Program.Players)
                        {
                            Program.SendStringData(player, DataHandler.Condense(DataHandler.MovementInput, details)); // + "|" + args.Player.Id));
                        }
                    }
                    else if (prefix.Equals(DataHandler.MovementJump)) // player jumped
                    {
                        foreach (NetworkUser player in Program.Players)
                        {
                            Program.SendStringData(player, DataHandler.Condense(DataHandler.MovementJump, args.Player.Id.ToString()));
                        }
                    }
                    else if (prefix.Equals(DataHandler.MovementRoll)) // player jumped
                    {
                        foreach (NetworkUser player in Program.Players)
                        {
                            Program.SendStringData(player, DataHandler.Condense(DataHandler.MovementRoll, args.Player.Id.ToString()));
                        }
                    }*/
                }
                else if (prefix.Equals(PacketDataUtils.SecureConnectionEstablished))
                {
                    user.SecureConnectionEstablished = true;
                    ConsoleLog.WriteIOGeneral("Secure connection successfully established with " + user.Connection.Socket.RemoteEndPoint);
                }
            }
        }
    }
}

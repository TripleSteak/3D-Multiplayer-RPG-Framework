using Final_Aisle_Server.Data;
using Final_Aisle_Server.Network.Entities;
using Final_Aisle_Server.Network.EventArgs;
using System;

using static Final_Aisle_Shared.Network.PacketDataUtils;

/**
 * Handles how to respond to user messages
 * 
 * Selection statements are in order of frequency
 */
namespace Final_Aisle_Server.Network
{
    public static class PacketProcessor
    {
        public static void ParseInput(PacketEventArgs args)
        {
            var user = args.User; // user who sent the packet
            var packet = args.Packet;
            var composite = packet.GetComposite(); // may be null, use only when you know the packet is of type "composite"
            var key = packet.GetKey(); // key, existence is universal across all packet types

            if (user.SecureConnectionEstablished)
            { // data can be safely transported, 
                if (user.LoggedIn)
                { // actions where player is already logged in
                    if (key.Equals(TransformPosition)) PlayerHandler.TransformPosition(user, composite.GetFloat(0), composite.GetFloat(1), composite.GetFloat(2));
                    else if (key.Equals(TransformRotation)) PlayerHandler.TransformRotation(user, packet.GetFloat());
                    else if (key.Equals(MovementInput)) PlayerHandler.MovementInput(user, composite.GetFloat(0), composite.GetFloat(1));
                    else if (key.Equals(MovementRoll)) PlayerHandler.MovementRoll(user, packet.GetFloat());
                    else if (key.Equals(MovementJump)) PlayerHandler.MovementJump(user);
                    else if (key.Equals(MovementToggleProne)) PlayerHandler.MovementToggleProne(user, packet.GetBool());
                    else if (key.Equals(PlayerPostConnect)) PlayerHandler.PostLogIn(user);
                }
                else if (key.Equals(TryLogin))
                {
                    string identifier = composite.GetString(0);
                    string password = composite.GetString(1);

                    bool successful = true;
                    string accountUUID = AccountDataHandler.GetUUIDFromEmail(identifier);
                    if (String.IsNullOrEmpty(accountUUID)) accountUUID = AccountDataHandler.GetUUIDFromUsername(identifier); // check username if email doesn't exist
                    if (String.IsNullOrEmpty(accountUUID)) successful = false; // neither email nor username exists

                    if (successful)
                    { // valid username/email
                        successful = AccountDataHandler.CheckPassword(accountUUID, password);

                        if (successful)
                        { // correct password
                            user.UserAccount = UserAccount.LoadAccount(accountUUID);

                            Program.SendEmpty(user, LoginSuccess);
                            PlayerHandler.LogIn(user);

                            return;
                        }
                    }

                    // login credentials were incorrect somewhere
                    Program.SendEmpty(user, LoginFail);
                    return;
                }
                else if (key.Equals(TryNewAccount))
                {
                    string email = composite.GetString(0);
                    string username = composite.GetString(1);
                    string password = composite.GetString(2);

                    ConsoleLog.WriteSmallIO("User from " + user.Connection.Socket.RemoteEndPoint + " would like to create an account with the following details:");
                    ConsoleLog.WriteSmallIO("\t   Email: " + email);
                    ConsoleLog.WriteSmallIO("\tUsername: " + username);
                    ConsoleLog.WriteSmallIO("\tPassword: " + new string('*', password.Length));

                    if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromEmail(email)))
                    {
                        Program.SendEmpty(user, EmailAlreadyTaken); // email exists
                        ConsoleLog.WriteSmallIO("Email already taken!");
                    }
                    else if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromUsername(username)))
                    {
                        Program.SendEmpty(user, UsernameAlreadyTaken); // username exists
                        ConsoleLog.WriteSmallIO("Username already taken!");
                    }
                    else
                    {
                        Program.SendEmpty(user, EmailVerifySent);

                        string verifyCode = "";
                        if (AccountDataHandler.RegisterData.ContainsKey(user)) verifyCode = AccountDataHandler.RegisterData[user].Item4;
                        if (String.IsNullOrEmpty(verifyCode)) verifyCode = AccountDataHandler.GenerateVerificationCode();

                        AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(email, username, password, verifyCode, AccountDataHandler.TotalVerifyEmailTries); // store temporary login information

                        // Attempt to send verification email to listed email address
                        EmailHandler.SendVerificationMail(email, username, verifyCode);
                    }

                }
                else if (key.Equals(TryVerifyEmail))
                {
                    if (!AccountDataHandler.RegisterData.ContainsKey(user) || !packet.GetString().Equals(AccountDataHandler.RegisterData[user].Item4, StringComparison.InvariantCultureIgnoreCase))
                    { // verification attempt failed
                        ConsoleLog.WriteSmallIO("User at email " + AccountDataHandler.RegisterData[user].Item1 + " failed verification with attempted code " + packet.GetString());
                        AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3, AccountDataHandler.RegisterData[user].Item4, AccountDataHandler.RegisterData[user].Item5 - 1);
                        if (AccountDataHandler.RegisterData[user].Item5 <= 0) // out of tries
                        {
                            AccountDataHandler.RegisterData.Remove(user);
                            Program.SendInt(user, EmailVerifyFail, 0); // 0 tries left
                        }
                        else Program.SendInt(user, EmailVerifyFail, AccountDataHandler.RegisterData[user].Item5); // send # tries left
                    }
                    else
                    { // verification attempt succeeded
                        ConsoleLog.WriteSmallIO("User at email " + AccountDataHandler.RegisterData[user].Item1 + " succeeded in email verification with code " + packet.GetString());
                        Program.SendEmpty(user, EmailVerifySuccess);

                        // save new account credentials
                        AccountDataHandler.CreateNewAccount(user, AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3);
                        AccountDataHandler.RegisterData.Remove(user);

                        PlayerHandler.LogIn(user);
                    }
                }
            }
            else if (key.Equals(SecureConnectionEstablished))
            { // AES system established
                user.SecureConnectionEstablished = true;
                ConsoleLog.WriteSmallIO("Secure connection successfully established with " + user.Connection.Socket.RemoteEndPoint);
            }
        }

    }
}

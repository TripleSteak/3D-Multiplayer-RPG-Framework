using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.LocalData
{
    public class UserAccount
    {
        /*
         * AccountUUID is a unique 128-bit one-time generation for each account
         * 
         * Email and password is not stored locally, but rather re-retrieved from storage when necessary
         */
        public string AccountUUID { get; }
        public string Username { get; }

        public UserAccount(string accountUUID, string username)
        {
            this.AccountUUID = accountUUID;
            this.Username = username;
        }

        public static UserAccount NewAccount(string email, string username, string password)
        {
            UserAccount account = new UserAccount(AccountDataHandler.GenerateNewAccountUUID(), username);

            AccountDataHandler.AddToEmailUsernameLists(email, username, account.AccountUUID);
            AccountDataHandler.SaveUserData(account, email, password);

            return account;
        }
    }
}

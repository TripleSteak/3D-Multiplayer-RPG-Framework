using FinalAisle_Server.Networking;
using FinalAisle_Shared.Networking;
using FinalAisle_Shared.Networking.Packet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FinalAisle_Server.LocalData
{
    public static class AccountDataHandler
    {

        private static List<Tuple<string, string>> EmailList = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> UsernameList = new List<Tuple<string, string>>();

        // stores various data relating to the new account creation process (email, username, password, verification code, # tries left)
        public static Dictionary<NetworkUser, Tuple<string, string, string, string, int>> RegisterData = new Dictionary<NetworkUser, Tuple<string, string, string, string, int>>();

        public static readonly int TotalVerifyEmailTries = 5;

        public static void Init()
        {
            string[] emailString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Email List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string[] usernameString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Username List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string s in emailString) if (!String.IsNullOrEmpty(s)) EmailList.Add(new Tuple<string, string>(s.Substring(0, s.IndexOf(' ')), s.Substring(s.IndexOf(' ') + 1)));
            foreach (string s in usernameString) if (!String.IsNullOrEmpty(s)) UsernameList.Add(new Tuple<string, string>(s.Substring(0, s.IndexOf(' ')), s.Substring(s.IndexOf(' ') + 1)));

            ConsoleLog.WriteDataGeneral("Loaded in " + EmailList.Count + " current email addresses and " + UsernameList.Count + " active usernames.");
        }

        public static string GenerateNewAccountUUID()
        {
            string UUID, newDir;
            bool repeat = false;

            do
            {
                UUID = Guid.NewGuid().ToString();
                newDir = Path.Combine(FileUtils.AccountsDirectory, UUID);
                repeat = Directory.Exists(newDir);

            } while (repeat);

            Directory.CreateDirectory(newDir);

            return UUID;
        }

        /**
         * Gets the corresponding account UUID from email
         */
        public static string GetUUIDFromEmail(string email)
        {
            if (EmailList.Count == 0) return "";

            int left = 0, right = EmailList.Count;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) > 0) right = mid;
                else if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) < 0) left = mid;
                else return "";
            }

            return EmailList[left].Item1.ToUpper().Equals(email.ToUpper()) ? EmailList[left].Item2 : "";
        }

        /**
         * Gets the corresponding account UUID from username
         */
        public static string GetUUIDFromUsername(string username)
        {
            if (UsernameList.Count == 0) return "";

            int left = 0, right = UsernameList.Count;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) > 0) right = mid;
                else if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) < 0) left = mid;
                else return "";
            }

            return UsernameList[left].Item1.ToUpper().Equals(username.ToUpper()) ? UsernameList[left].Item2 : "";
        }

        public static void AddToEmailUsernameLists(string email, string username, string accountUUID)
        {
            int left = 0, right = EmailList.Count;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) >= 0) right = mid;
                else left = mid;
            }
            EmailList.Insert(left, new Tuple<string, string>(email, accountUUID));

            left = 0;
            right = UsernameList.Count;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) >= 0) right = mid;
                else left = mid;
            }
            UsernameList.Insert(left, new Tuple<string, string>(username, accountUUID));

            SaveEmailUsernameLists();
        }

        public static void CreateNewAccount(NetworkUser user, string email, string username, string password)
        {
            UserAccount newAccount = UserAccount.NewAccount(email, username, password);
            user.UserAccount = newAccount;
        }

        public static string GenerateVerificationCode()
        {
            string verificationCode = "";
            Random rand = new Random();

            for (int i = 0; i < 6; i++)
            { // Generates random characters for verification code
                int num = (i == 2 ? rand.Next(10) : rand.Next(36));
                if (num < 10)
                    verificationCode += num.ToString();
                else
                {
                    num -= 10;
                    verificationCode += ((char)(num + 'A')).ToString();
                }
            }

            return verificationCode;
        }

        public static void SaveUserData(UserAccount account, string email, string password)
        {
            string accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, account.AccountUUID);
            FileUtils.WriteToFile(accountFolder, "Email", email);
            FileUtils.WriteToFile(accountFolder, "Password", password);
        }

        private static void SaveEmailUsernameLists()
        {
            string emailString = "";
            foreach (Tuple<string, string> t in EmailList) emailString += t.Item1 + " " + t.Item2 + Environment.NewLine;
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Email List", emailString);

            string usernameString = "";
            foreach (Tuple<string, string> t in UsernameList) usernameString += t.Item1 + " " + t.Item2 + Environment.NewLine;
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Username List", usernameString);
        }
    }
}

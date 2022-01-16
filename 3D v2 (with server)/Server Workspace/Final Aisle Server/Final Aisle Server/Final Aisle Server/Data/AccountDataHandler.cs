using Final_Aisle_Server.Network;
using System;
using System.Collections.Generic;
using System.IO;

/**
 * Handles all data processing involved in user accounts and account credentials
 */
namespace Final_Aisle_Server.Data
{
    public static class AccountDataHandler
    {

        private static List<Tuple<string, string>> EmailList = new List<Tuple<string, string>>(); // <email, account UUID>
        private static List<Tuple<string, string>> UsernameList = new List<Tuple<string, string>>(); // <username, account UUID>

        // stores various data relating to the new account creation process (email, username, password, verification code, # tries left)
        public static Dictionary<NetworkUser, Tuple<string, string, string, string, int>> RegisterData = new Dictionary<NetworkUser, Tuple<string, string, string, string, int>>();

        public static readonly int TotalVerifyEmailTries = 5; // maximum number of verification attempts allowed

        public static void Init()
        {
            string[] emailString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Email List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string[] usernameString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Username List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 1; i < emailString.Length; i += 2)
            {
                string email = emailString[i - 1];
                string accountID = emailString[i];
                if (!String.IsNullOrEmpty(email)) EmailList.Add(new Tuple<string, string>(email, accountID));
            }
            for (int i = 1; i < usernameString.Length; i += 2)
            {
                string username = usernameString[i - 1];
                string accountID = usernameString[i];
                if (!String.IsNullOrEmpty(username)) UsernameList.Add(new Tuple<string, string>(username, accountID));
            }

            ConsoleLog.WriteSmallData("Loaded in " + EmailList.Count + " current email addresses and " + UsernameList.Count + " active usernames.");
        }

        public static string GenerateNewAccountUUID()
        {
            string UUID, newDir;
            bool repeat;

            do
            {
                UUID = Guid.NewGuid().ToString();
                newDir = Path.Combine(FileUtils.AccountsDirectory, UUID);
                repeat = Directory.Exists(newDir);

            } while (repeat); // prevent overlapping UUIDs, though the chance is very slim

            Directory.CreateDirectory(newDir);

            return UUID;
        }

        /**
         * Gets the corresponding account UUID from email (binary search)
         */
        public static string GetUUIDFromEmail(string email)
        {
            if (EmailList.Count == 0) return "";

            int left = 0, right = EmailList.Count;
            while (right != left)
            {
                int mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) > 0) right = mid;
                else if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) < 0) left = mid + 1;
                else return EmailList[mid].Item2;
            }

            return "";
        }

        /**
         * Gets the corresponding account UUID from username (binary search)
         */
        public static string GetUUIDFromUsername(string username)
        {
            if (UsernameList.Count == 0) return "";

            int left = 0, right = UsernameList.Count;
            while (right != left)
            {
                int mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) > 0) right = mid;
                else if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) < 0) left = mid + 1;
                else return UsernameList[mid].Item2;
            }

            return "";
        }

        /**
         * Adds a new account entry into the email and username lists (binary insertion)
         */
        public static void AddToEmailUsernameLists(string email, string username, string accountUUID)
        {
            int left = 0, right = EmailList.Count;
            while (right != left)
            {
                int mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) >= 0) right = mid;
                else left = mid + 1;
            }
            EmailList.Insert(left, new Tuple<string, string>(email, accountUUID));

            left = 0;
            right = UsernameList.Count;
            while (right != left)
            {
                int mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) >= 0) right = mid;
                else left = mid + 1;
            }
            UsernameList.Insert(left, new Tuple<string, string>(username, accountUUID));

            SaveEmailUsernameLists();
        }

        /**
         * Register a new account (and its credentials) into the database
         */
        public static void CreateNewAccount(NetworkUser user, string email, string username, string password)
        {
            UserAccount newAccount = UserAccount.NewAccount(email, username, password);
            user.UserAccount = newAccount;
        }

        /**
         * Generate a random six-digit alphanumeric verification code to be sent to the user by email
         */
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

        public static void SaveUserData(UserAccount account, string email, string username, string password)
        {
            string accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, account.AccountUUID);
            FileUtils.WriteToFile(accountFolder, "Email", email);
            FileUtils.WriteToFile(accountFolder, "Username", username);
            FileUtils.WriteToFile(accountFolder, "Password", password);
        }

        public static string GetUsername(string accountUUID)
        {
            string accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, accountUUID);
            return FileUtils.ReadStringFromFile(accountFolder, "Username");
        }

        /**
         * Password verification during login
         */
        public static bool CheckPassword(string accountUUID, string password)
        {
            string accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, accountUUID);
            return FileUtils.ReadStringFromFile(accountFolder, "Password").Equals(password);
        }

        private static void SaveEmailUsernameLists()
        {
            string emailString = "";
            foreach (Tuple<string, string> t in EmailList) emailString += t.Item1 + Environment.NewLine + t.Item2 + Environment.NewLine;
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Email List", emailString);

            string usernameString = "";
            foreach (Tuple<string, string> t in UsernameList) usernameString += t.Item1 + Environment.NewLine + t.Item2 + Environment.NewLine;
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Username List", usernameString);
        }
    }
}

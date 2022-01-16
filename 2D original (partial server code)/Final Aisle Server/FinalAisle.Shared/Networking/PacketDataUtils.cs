using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Shared.Networking
{
    public static class PacketDataUtils
    {
        /*
         * Account/network logistics prefixes
         */
        public const string SecureConnectionEstablished = "SecureConnectionEstablished";

        public const string TryNewAccount = "TryNewAccount";
        public const string TryVerifyEmail = "TryVerifyEmail";

        public const string EmailAlreadyTaken = "EmailAlreadyTaken";
        public const string UsernameAlreadyTaken = "UsernameAlreadyTaken";
        public const string EmailVerifySent = "EmailVerifySent";

        public const string EmailVerifySuccess = "EmailVerifySuccess";
        public const string EmailVerifyFail = "EmailVerifyFail";


        /*
         * In-game data prefixes
         */
        public const string JoinLevel = "JL"; // client joins a new level

        public const string MovementInput = "M"; // player's horizontal motion input
        public const string MovementJump = "MJ"; // player jumped
        public const string MovementRoll = "MR"; // player rolled

        public static string Condense(string prefix, string data) => prefix + ":" + data;
        public static string GetPrefix(string condensed) => condensed.Substring(0, condensed.IndexOf(':'));
        public static string GetStringData(string condensed) => condensed.Substring(condensed.IndexOf(':') + 1);
    }
}

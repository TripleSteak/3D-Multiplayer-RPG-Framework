/**
 * Contains the keys for classifying all server/client communication commands
 * 
 * Each command must be accompanied by a comment that starts with an identification of the message sender (client, server, or 2-way)
 */
namespace Final_Aisle_Shared.Network
{
    public static class PacketDataUtils
    {
        /*
         * Account/network logistics keys
         */
        public const string SecureConnectionEstablished = "SecureConnectionEstablished"; // 2-way: AES keys successfully established

        public const string TryNewAccount = "TryNewAccount"; // client: information for account creation
        public const string TryVerifyEmail = "TryVerifyEmail"; // client: verification code to check for match
        public const string TryLogin = "TryLogin"; // client: login credentials

        public const string EmailAlreadyTaken = "EmailAlreadyTaken"; // server: that the registering email is already registered
        public const string UsernameAlreadyTaken = "UsernameAlreadyTaken"; // server: that the registering username is already registered
        public const string EmailVerifySent = "EmailVerifySent"; // server: that the verification email with the code has been sent to the client

        public const string EmailVerifySuccess = "EmailVerifySuccess"; // server: that the user's verification code matches
        public const string EmailVerifyFail = "EmailVerifyFail"; // server: that the user's verfication code doesn't match

        public const string LoginSuccess = "LoginSuccess"; // server: that the user's login has succeeded
        public const string LoginFail = "LoginFail"; // server: that username/password is wrong

        /*
         * Player connect/disconnect
         */
        public const string PlayerConnected = "PlayerConnected"; // server: that a new player has joined, attached with a username
        public const string PlayerDisconnected = "PlayerDisconnected"; // server: that a player has left, attached with a username
        public const string PlayerPostConnect = "PlayerPostConnect"; // client: the player has successfully logged in, as a response

        /*
         * Player character data
         */
        public const string CharacterInfo = "CharacterInfo"; // server: character data of the character the player chose to play

        /*
         * In-game player movements and actions prefixes
         */
        public const string MovementInput = "M"; // 2-way: changes in player movement input, as a 2D vector
        public const string MovementJump = "MJ"; // 2-way: player jumped
        public const string MovementRoll = "MR"; // 2-way: player rolled
        public const string MovementToggleProne = "MP"; // 2-way: player toggled prone

        public const string TransformPosition = "P"; // 2-way: periodic checks in player position, as a Vector3
        public const string TransformRotation = "R"; // 2-way: changes in player rotation, as a float        
    }
}

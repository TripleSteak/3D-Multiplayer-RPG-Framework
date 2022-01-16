/**
 * Event object related to user interactions
 */
namespace Final_Aisle_Server.Network.EventArgs
{
    public class UserEventArgs
    {
        public NetworkUser User { get; set; }

        public UserEventArgs(NetworkUser user) => this.User = user;
    }
}

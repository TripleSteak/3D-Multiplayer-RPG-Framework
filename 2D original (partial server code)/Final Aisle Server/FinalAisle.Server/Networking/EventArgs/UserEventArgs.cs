using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalAisle_Server.Networking.EventArgs
{
    public class UserEventArgs
    {
        public NetworkUser User { get; set; }

        public UserEventArgs(NetworkUser User)
        {
            this.User = User;
        }
    }
}

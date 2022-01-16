using Final_Aisle_Server.Data;
using Final_Aisle_Server.Network;
using Final_Aisle_Server.Network.Entities;
using Final_Aisle_Server.Network.EventArgs;
using Final_Aisle_Shared.Network.Packet;
using System;
using System.Collections.Generic;
using System.Threading;

/**
 * Main method for the Final Aisle centralized server
 */
namespace Final_Aisle_Server
{
    class Program
    {
        public static List<NetworkUser> Users = new List<NetworkUser>(); // list of all active client connections
        private static AsyncSocketListener Listener = new AsyncSocketListener();
        private static string ApplicationName = "Final Aisle";

        static void Main(string[] args)
        {
            Listener.OnUserConnected += (object sender, UserEventArgs e) =>
            {
                ConsoleLog.WriteSmallIO("Client #" + e.User.ID + " has joined from " + e.User.Connection.Socket.RemoteEndPoint);
                Users.Add(e.User);
            };

            Listener.OnUserDisconnected += (object sender, UserEventArgs e) =>
            {
                ConsoleLog.WriteSmallIO("Client #" + e.User.ID + " disconnected");
                Users.Remove(e.User);
                PlayerHandler.LogOut(e.User);
            };

            Listener.OnPacketReceived += (object sender, PacketEventArgs e) => PacketProcessor.ParseInput(e);

            long lastTotalReceived = 0;
            long lastTotalSent = 0;

            System.Timers.Timer stats = new System.Timers.Timer(1000);
            stats.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                long currentReceived = Listener.TotalBytesReceived - lastTotalReceived;
                lastTotalReceived = Listener.TotalBytesReceived;

                long currentSent = Listener.TotalBytesSent - lastTotalSent;
                lastTotalSent = Listener.TotalBytesSent;

                double kbSent = Listener.TotalBytesSent / 1000.0;
                double kbReceived = Listener.TotalBytesReceived / 1000.0;

                double kbSecSent = currentSent / 1000.0;
                double kbSecReceived = currentReceived / 1000.0;

                string statString = $"Send: {kbSent.ToString("N2")} kb ({kbSecSent.ToString("N2")} kb/s) | Receive: {kbReceived.ToString("N2")} kb ({kbSecReceived.ToString("N2")} kb/s)";
                Console.Title = ApplicationName + " Server | " + statString;
            };
            stats.Start();

            ThreadStart listenRef = new ThreadStart(Listener.StartListening);
            Thread listenThread = new Thread(listenRef);
            listenThread.Start();

            // Initialize storage accessors/handlers
            AccountDataHandler.Init();

            while (true)
            {
                string command = Console.ReadLine();
                bool success = ExecuteConsoleCommand(command);

                if (!success) ConsoleLog.WriteInfo("Unrecognized command! Type \"help\" for a list of commands.");
            };
        }
        private static bool ExecuteConsoleCommand(string command)
        {
            string[] split = command.Split(' '); // separate into arguments
            if (split[0].Equals("help", StringComparison.InvariantCultureIgnoreCase))
            {
                ConsoleLog.WriteInfo("Displaying a list of available commands:");
                ConsoleLog.WriteInfo("\thelp\t\t:\tShows a list of available console commands.");
                ConsoleLog.WriteInfo("\tshutdown\t:\tShuts down the server.");
                return true;
            }
            else if (split[0].Equals("shutdown", StringComparison.InvariantCultureIgnoreCase))
            {
                ConsoleLog.WriteBigStatus("Shutting down...");
                Thread.Sleep(1000);
                System.Environment.Exit(0);
            }
            return false;
        }

        public static void SendBool(NetworkUser user, string key, bool value) => Listener.Send(user, new Packet(PacketType.Boolean, new BooleanPacketData(key, value)));
        public static void SendComposite(NetworkUser user, string key, List<object> values) => Listener.Send(user, new Packet(PacketType.Composite, new CompositePacketData(key, values)));
        public static void SendDouble(NetworkUser user, string key, double value) => Listener.Send(user, new Packet(PacketType.Double, new DoublePacketData(key, value)));
        public static void SendEmpty(NetworkUser user, string key) => Listener.Send(user, new Packet(PacketType.Empty, new EmptyPacketData(key)));
        public static void SendEnum(NetworkUser user, string key, object value) => Listener.Send(user, new Packet(PacketType.Enum, new EnumPacketData(key, value)));
        public static void SendFloat(NetworkUser user, string key, float value) => Listener.Send(user, new Packet(PacketType.Float, new FloatPacketData(key, value)));
        public static void SendInt(NetworkUser user, string key, int value) => Listener.Send(user, new Packet(PacketType.Integer, new IntegerPacketData(key, value)));
        public static void SendString(NetworkUser user, string key, string value) => Listener.Send(user, new Packet(PacketType.String, new StringPacketData(key, value)));
    }
}

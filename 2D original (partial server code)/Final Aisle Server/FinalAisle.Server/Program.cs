using FinalAisle_Server.Networking;
using FinalAisle_Server.Networking.EventArgs;
using FinalAisle_Shared.Networking.Packet;
using FinalAisle_Shared.World;
using FinalAisle_Server.LocalData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FinalAisle_Server
{
    class Program
    {
        public static List<NetworkUser> Players = new List<NetworkUser>();
        private static DateTime startTime = DateTime.Now;
        private static AsyncSocketListener listener = new AsyncSocketListener();

        static void Main(string[] args)
        {
            listener.OnUserConnected += (object sender, UserEventArgs e) =>
            {
                ConsoleLog.WriteIOGeneral("A client has connected from " + e.User.Connection.Socket.RemoteEndPoint);
                Players.Add(e.User);
            };

            listener.OnUserDisconnected += (object sender, UserEventArgs e) =>
            {
                ConsoleLog.WriteIOGeneral("Client #" + e.User.Id + " has disconnected (" + e.User.Connection.Socket.RemoteEndPoint + ")");
                Players.Remove(e.User);
            };

            listener.OnPacketReceived += (object sender, PacketEventArgs e) => PacketProcessor.ParseInput(e);

            long lastTotalReceived = 0;
            long lastTotalSent = 0;

            System.Timers.Timer stats = new System.Timers.Timer(1000);
            stats.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                long currentReceived = listener.TotalBytesReceived - lastTotalReceived;
                lastTotalReceived = listener.TotalBytesReceived;

                long currentSent = listener.TotalBytesSent - lastTotalSent;
                lastTotalSent = listener.TotalBytesSent;

                double kbSent = listener.TotalBytesSent / 1000.0;
                double kbReceived = listener.TotalBytesReceived / 1000.0;

                double kbSecSent = currentSent / 1000.0;
                double kbSecReceived = currentReceived / 1000.0;

                string statString = $"Send: {kbSent.ToString("N2")} KB ({kbSecSent.ToString("N2")} KB/s) | Receive: {kbReceived.ToString("N2")} KB ({kbSecReceived.ToString("N2")} KB/s)";
                Console.Title = "Final Aisle Server | " + statString;
            };
            stats.Start();

            ThreadStart listenRef = new ThreadStart(listener.StartListening);
            Thread listenThread = new Thread(listenRef);
            listenThread.Start();

            // Initialize storage accessors/handlers
            AccountDataHandler.Init();

            // Load game data
            Level.InitLevels();

            // Allow console commands
            Thread.Sleep(1000);
            while (true)
            {
                string command = Console.ReadLine();
                ExecuteConsoleCommand(command);
            }
        }

        private static void ExecuteConsoleCommand(string line)
        {
            string[] args = line.Split(' ');
            if (args[0].Equals("shutdown", StringComparison.InvariantCultureIgnoreCase))
            {
                // Perform shutdown routine

                ConsoleLog.WriteServerImportant("Shutting down...");
                Thread.Sleep(2000);
                Environment.Exit(0);
            } else
            {
                ConsoleLog.WriteInfo("Unrecognized command.");
            }
        }

        public static void SendStringData(NetworkUser player, string data)
        {
            listener.Send(player, new Packet { Type = PacketType.Message, Data = new MessagePacketData(data) });
        }
    }
}

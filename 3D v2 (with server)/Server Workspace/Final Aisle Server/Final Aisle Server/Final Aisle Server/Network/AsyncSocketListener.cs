using Final_Aisle_Server.Network.EventArgs;
using Final_Aisle_Shared.Network.Packet;
using Final_Aisle_Shared.Network;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

/**
 * Object that listens for messages on the network
 */
namespace Final_Aisle_Server.Network
{
    public class AsyncSocketListener
    {
        private ManualResetEvent allDone = new ManualResetEvent(false);

        public event UserConnectedEvent OnUserConnected;
        public event UserDisconnectedEvent OnUserDisconnected;
        public event PacketReceivedEvent OnPacketReceived;
        public event PacketSentEvent OnPacketSent;

        public long TotalBytesSent = 0; // accumulator for all network output
        public long TotalBytesReceived = 0; // accumulator for all network input

        public const int Port = 8031; // make sure to port forward!

        public AsyncSocketListener() { }

        public void StartListening()
        {
            IPAddress ip = IPAddress.Any;
            IPEndPoint localEndpoint = new IPEndPoint(ip, Port);

            Socket listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // create socket

            try
            {
                listener.Bind(localEndpoint);
                listener.Listen(100);
                ConsoleLog.WriteBigStatus("Final Aisle listening server started on port " + Port + ".");

                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteSmallError(ex.ToString());
            }
        }

        public void Send(NetworkUser user, Packet obj)
        {
            byte[] byteData = obj.Serialize(user.UserAES);

            try
            {
                Socket socket = user.Connection.Socket;
                socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), new SendCallbackArgs { User = user, Packet = obj });
            }
            catch (SocketException) { } // socket closed
        }

        /**
         * Found client trying to connect
         */
        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            NetworkUser user = new NetworkUser();
            user.Connection.Socket = handler;
            handler.BeginReceive(user.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), user);
            OnUserConnected?.Invoke(this, new UserEventArgs(user));
        }

        /**
         * Accepts and reads data sent from client
         */
        private void ReadCallback(IAsyncResult ar)
        {
            NetworkUser user = (NetworkUser)ar.AsyncState;
            Socket handler = user.Connection.Socket;

            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }

            TotalBytesReceived += bytesRead; // add to total

            if (bytesRead > 0)
            { // non-empty input from network
                user.Connection.Message.AddRange(user.Connection.Buffer.Take(bytesRead));

                int byteCount = BitConverter.ToInt32(user.Connection.Message.Take(sizeof(Int32)).ToArray(), 0);
                while (user.Connection.Message.Count >= byteCount + sizeof(Int32))
                { // read as many docked packets as possible (e.g. if 2 are available, read both)
                    if (user.AESKeySent)
                    { // receive message as normal
                        try
                        {
                            Packet p = PacketSerializer.Deserialize(user.Connection.Message.Take(byteCount + sizeof(Int32)), user.UserAES); // don't overshoot the amount of bytes sent to the packet
                            OnPacketReceived?.Invoke(this, new PacketEventArgs { Packet = p, User = user });
                        }
                        catch (InvalidDataException)
                        {
                            ConsoleLog.WriteSmallError("Could not parse message sent by " + user.UserAccount.Username + ": invalid data exception (message length: " + byteCount + ")"); // unknown cause
                        }
                    }
                    else
                    { // if AES key has not been sent to client yet, send AES key to client
                        var byteData = user.Connection.Message.Skip(sizeof(Int32)).Decompress().ToArray();
                        var publicKeyString = Encoding.ASCII.GetString(byteData);

                        // Convert string into RSA public key
                        var sr = new StringReader(publicKeyString);
                        var xs = new XmlSerializer(typeof(RSAParameters));
                        var publicKey = (RSAParameters)xs.Deserialize(sr);

                        // Load public key parameters
                        var csp = new RSACryptoServiceProvider(2048);
                        csp.ImportParameters(publicKey);

                        var aesKey = user.UserAES.Key;
                        var cipherText = csp.Encrypt(aesKey, false).Compress().PrependLength().ToArray();

                        // Send to client
                        user.Connection.Socket.BeginSend(cipherText, 0, cipherText.Length, SocketFlags.None, new AsyncCallback(SendCallback), new SendCallbackArgs { User = user, Packet = null });

                        user.AESKeySent = true;
                    }
                    user.Connection.Message = user.Connection.Message.Skip(byteCount + sizeof(Int32)).ToList(); // skip over already-read bytes

                    if (user.Connection.Message.Count >= sizeof(Int32)) // only reset byte count if the byte buffer hasn't been cleared
                        byteCount = BitConverter.ToInt32(user.Connection.Message.Take(sizeof(Int32)).ToArray(), 0); // reset byte count for next block
                }

                try
                {
                    handler.BeginReceive(user.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), user);
                }
                catch (SocketException)
                {
                    ConsoleLog.WriteSmallError("Error occurred while communicating with user #" + user.ID + " from " + user.Connection.Socket.RemoteEndPoint + ".");
                }
            }
            else
            {
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
                handler.Close();
            }
        }

        /**
         * Sends data to client
         */
        private void SendCallback(IAsyncResult ar)
        {
            SendCallbackArgs args = (SendCallbackArgs)ar.AsyncState;

            NetworkUser user = args.User;
            Socket socket = user.Connection.Socket;
            int bytesSent = 0;

            try
            {
                bytesSent = socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
            }

            TotalBytesSent += bytesSent; // add to total
            OnPacketSent?.Invoke(this, new PacketEventArgs { User = user, Packet = args.Packet });
        }
        private class SendCallbackArgs
        {
            public NetworkUser User { get; internal set; }
            public Packet Packet { get; internal set; }
        }

        public delegate void UserConnectedEvent(object sender, UserEventArgs user);
        public delegate void UserDisconnectedEvent(object seender, UserEventArgs user);
        public delegate void PacketReceivedEvent(object sender, PacketEventArgs args);
        public delegate void PacketSentEvent(object sender, PacketEventArgs args);
    }
}

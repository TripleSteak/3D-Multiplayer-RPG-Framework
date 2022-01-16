using FinalAisle_Server.Networking.EventArgs;
using FinalAisle_Shared.Networking;
using FinalAisle_Shared.Networking.Packet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FinalAisle_Server.Networking
{
    public class AsyncSocketListener
    {
        public ManualResetEvent allDone = new ManualResetEvent(false);

        public event UserConnectedEvent OnUserConnected;
        public event UserDisconnectedEvent OnUserDisconnected;
        public event PacketReceivedEvent OnPacketReceived;
        public event PacketSentEvent OnPacketSent;

        public long TotalBytesSent = 0;
        public long TotalBytesReceived = 0;

        private const int port = 8032;

        public AsyncSocketListener()
        {

        }

        public void StartListening()
        {
            IPAddress ip = IPAddress.Any;
            IPEndPoint localEndpoint = new IPEndPoint(ip, port);

            Socket listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndpoint);
                listener.Listen(100);
                Console.ForegroundColor = ConsoleColor.Yellow;
                ConsoleLog.WriteServerImportant("Final Aisle listening server started.");
                Console.ForegroundColor = ConsoleColor.White;

                while(true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Send(NetworkUser User, Packet obj)
        {
            byte[] byteData = obj.Serialize(User.UserAES);

            Socket socket = User.Connection.Socket;
            socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), new SendCallbackArgs { User = User, Packet = obj });
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            NetworkUser User = new NetworkUser();
            User.Connection.Socket = handler;
            handler.BeginReceive(User.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), User);
            OnUserConnected?.Invoke(this, new UserEventArgs(User));
        }

        private void ReadCallback(IAsyncResult ar)
        {
            NetworkUser User = (NetworkUser)ar.AsyncState;
            Socket handler = User.Connection.Socket;

            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(User));
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }

            TotalBytesReceived += bytesRead;

            if (bytesRead > 0)
            {
                User.Connection.Message.AddRange(User.Connection.Buffer.Take(bytesRead));

                int byteCount = BitConverter.ToInt32(User.Connection.Message.Take(sizeof(Int32)).ToArray(), 0);
                if (User.Connection.Message.Count == byteCount + sizeof(Int32))
                {
                    if (User.AESKeySent)
                    { // receive message as normal
                        Packet p = Packet.Deserialize(User.Connection.Message, User.UserAES);
                        OnPacketReceived?.Invoke(this, new PacketEventArgs { Packet = p, User = User });
                    } else
                    { // if AES key has not been sent to client yet, send AES key to client
                        var byteData = User.Connection.Message.Skip(sizeof(int)).Decompress().ToArray();
                        var publicKeyString = Encoding.ASCII.GetString(byteData);

                        // Convert string into RSA public key
                        var sr = new StringReader(publicKeyString);
                        var xs = new XmlSerializer(typeof(RSAParameters));
                        var publicKey = (RSAParameters)xs.Deserialize(sr);

                        // Load public key parameters
                        var csp = new RSACryptoServiceProvider(2048);
                        csp.ImportParameters(publicKey);

                        var aesKey = User.UserAES.Key;
                        var cipherText = csp.Encrypt(aesKey, false).Compress().PrependLength().ToArray();

                        // Send to client
                        User.Connection.Socket.BeginSend(cipherText, 0, cipherText.Length, SocketFlags.None, new AsyncCallback(SendCallback), new SendCallbackArgs { User = User, Packet = null });

                        User.AESKeySent = true;
                    }
                    User.Connection.Message.Clear();
                }
                handler.BeginReceive(User.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), User);
            } else
            {
                OnUserDisconnected?.Invoke(this, new UserEventArgs(User));
                handler.Close();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            SendCallbackArgs args = (SendCallbackArgs)ar.AsyncState;

            NetworkUser User = args.User;
            Socket socket = User.Connection.Socket;
            int bytesSent = 0;

            try
            {
                bytesSent = socket.EndSend(ar);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(User));
            }

            TotalBytesSent += bytesSent;
            if(args.Packet != null) OnPacketSent?.Invoke(this, new PacketEventArgs { User = User, Packet = args.Packet });
        }

        private class SendCallbackArgs
        {
            public NetworkUser User { get; internal set; }
            public Packet Packet { get; internal set; }
        }

        public delegate void UserConnectedEvent(object sender, UserEventArgs User);
        public delegate void UserDisconnectedEvent(object seender, UserEventArgs User);
        public delegate void PacketReceivedEvent(object sender, PacketEventArgs args);
        public delegate void PacketSentEvent(object sender, PacketEventArgs args);
    }
}

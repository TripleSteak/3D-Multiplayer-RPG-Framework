using FinalAisle_Shared.Networking.Packet;
using FinalAisle_Shared.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class AsyncClient : IDisposable
{
    private const int port = 8032;
    private Socket client;

    public event PacketReceivedEvent OnPacketReceived;

    private SymmetricAlgorithm aes;
    private bool aesKeyReceived = false;
    private bool aesKeyRequested = false;

    private RSAParameters privateKey;
    private byte[] publicKeyBytes;

    private Connection connection;

    public AsyncClient(Connection connection)
    {
        this.connection = connection;

        StartClient();

        aes = new RijndaelManaged();
        aes.Padding = PaddingMode.PKCS7;
    }

    public void StartClient()
    {
        IPAddress ip = IPAddress.Parse("127.0.0.1"); // change to public network IP
        IPEndPoint remoteEndpoint = new IPEndPoint(ip, port);
        try
        {
            client = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.BeginConnect(remoteEndpoint, new AsyncCallback(ConnectCallback), client);

            // Generate RSA public-private key pair
            var csp = new RSACryptoServiceProvider(2048);
            privateKey = csp.ExportParameters(true);
            var publicKey = csp.ExportParameters(false);

            // Translate public key into string
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, publicKey);
            string publicKeyString = sw.ToString();

            // Convert string to bytes and send RSA public key to server
            publicKeyBytes = Encoding.ASCII.GetBytes(publicKeyString).Compress().PrependLength().ToArray();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    public void Send(Packet p)
    {
        if (aesKeyReceived)
        { // send message as normal
            var byteData = p.Serialize(aes);
            client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
        }
        else if (!aesKeyRequested)
        { // if no AES key present, send public RSA key to server to request
            client.BeginSend(publicKeyBytes, 0, publicKeyBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
            UnityEngine.Debug.Log("Sending RSA key to server...");
            aesKeyRequested = true;
        }
    }

    public void Receive(Socket client)
    {
        try
        {
            ServerConnection state = new ServerConnection();
            state.Socket = client;

            client.BeginReceive(state.Buffer, 0, ServerConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            UnityEngine.Debug.Log(string.Format("Socket connected to {0}", client.RemoteEndPoint.ToString()));
            Receive(client);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        ServerConnection state = (ServerConnection)ar.AsyncState;
        Socket client = state.Socket;

        int bytesRead = 0;

        try
        {
            bytesRead = client.EndReceive(ar);
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log("Cannot access a disposed socket.");
        }

        if (bytesRead > 0)
        {
            state.Message.AddRange(state.Buffer.Take(bytesRead));

            int byteCount = BitConverter.ToInt32(state.Message.Take(sizeof(Int32)).ToArray(), 0);
            if (state.Message.Count == byteCount + sizeof(int))
            {
                if (aesKeyReceived)
                { // receive message as normal
                    Packet p = Packet.Deserialize(state.Message, aes);
                    OnPacketReceived?.Invoke(this, new PacketReceivedEventArgs { Packet = p });
                } else
                { // if AES key is not received yet, this has to be the AES key!
                    var byteData = state.Message.Skip(sizeof(int)).Decompress().ToArray();

                    var csp = new RSACryptoServiceProvider();
                    csp.ImportParameters(privateKey);

                    aes.Key = csp.Decrypt(byteData, false);
                    aesKeyReceived = true;

                    // write connected established
                    connection.SendData(PacketDataUtils.Condense(PacketDataUtils.SecureConnectionEstablished, ""));
                }
                state.Message.Clear();
            }

            client.BeginReceive(state.Buffer, 0, ServerConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket)ar.AsyncState;
            int bytesSent = client.EndSend(ar);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    public void Shutdown()
    {
        try
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception)
        {
            // Client already closed, no additional shutdown process required
        }
    }

    public delegate void PacketReceivedEvent(object sender, PacketReceivedEventArgs e);

    #region IDisposable Support
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        try
        {
            if (!disposedValue)
            {
                if (disposing) Shutdown();
                disposedValue = true;
            }
        }
        catch (Exception)
        {
            // Object already disposed, no further action needed
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}

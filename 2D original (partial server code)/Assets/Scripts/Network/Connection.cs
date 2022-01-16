using Newtonsoft.Json;
using FinalAisle_Shared.Networking.Packet;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using UnityEngine;
using System;

public class Connection : MonoBehaviour
{
    private AsyncClient Client;
    private DataProcessor Processor;

    void Awake()
    {
        UnityThread.initUnityThread();
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        Processor = GetComponent<DataProcessor>();

        Client = new AsyncClient(this);
        Client.OnPacketReceived += (object sender, PacketReceivedEventArgs e) => Processor.ParseInput(e);
        SendData("Establishing connection..."); // attempt to send a message in order to set up secure connection
    }

    public void Connect()
    {
        Client.StartClient();
    }

    public void SendData(string data)
    {
        try
        {
            var packet = new Packet { Type = PacketType.Message, Data = new MessagePacketData(data) };
            Client.Send(packet);
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log("Data not sent to server... awaiting connection...");
        }
    }

    public void OnApplicationQuit()
    {
        Client.Dispose();
    }
}

using Final_Aisle_Shared.Network.Packet;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

/**
 * Connection instance between client and server
 * 
 * Also acts as a statically accessible class attached to the network connection object that moves through scenes
 * Connection class is used to 
 */
public class Connection : MonoBehaviour
{
    public static Connection instance; // access connection object statically

    private AsyncClient Client;
    [HideInInspector]
    public PacketProcessor PacketProcessor;
    [HideInInspector]
    public PrefabLibrary PrefabLibrary;
    [HideInInspector]
    public PlayerHandler PlayerHandler;

    [HideInInspector]
    public static bool LoggedIn = false; // change to true after getting logged in

    void Awake()
    {
        instance = this;
        UnityThread.InitUnityThread();
        DontDestroyOnLoad(this.gameObject);

        PacketProcessor = GetComponent<PacketProcessor>();
        PrefabLibrary = GetComponent<PrefabLibrary>();
        PlayerHandler = GetComponent<PlayerHandler>();
    }

    void Start()
    {
        Client = new AsyncClient(this);
        Client.OnPacketReceived += (object sender, PacketReceivedEventArgs e) => PacketProcessor.ParseInput(e);

        // attempt to send a message in order to set up secure connection
        Thread connectThread = new Thread(() =>
        {
            bool connected = false;
            while(!connected)
            {
                connected = SendEmpty(""); // heartbeat packet, used to check if connection was established
                Thread.Sleep(500);
            }
        });
        connectThread.Start();
    }

    public void Connect()
    {
        Client.StartClient();
    }

    /**
     * Attempts to send the given packet to the server
     * Call by public methods
     * 
     * @returns whether the data was successfully transmitted to the server
     */
    private bool SendData(Packet packet)
    {
        try
        {
            Client.Send(packet);
            return true;
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log("Data not sent to server... (connection could not be established?)");
        }
        return false;
    }

    public bool SendBool(string key, bool value) => SendData(new Packet(PacketType.Boolean, new BooleanPacketData(key, value)));
    public bool SendComposite(string key, List<object> values) => SendData(new Packet(PacketType.Composite, new CompositePacketData(key, values)));
    public bool SendDouble(string key, double value) => SendData(new Packet(PacketType.Double, new DoublePacketData(key, value)));
    public bool SendEmpty(string key) => SendData(new Packet(PacketType.Empty, new EmptyPacketData(key)));
    public bool SendEnum(string key, object value) => SendData(new Packet(PacketType.Enum, new EnumPacketData(key, value)));
    public bool SendFloat(string key, float value) => SendData(new Packet(PacketType.Float, new FloatPacketData(key, value)));
    public bool SendInt(string key, int value) => SendData(new Packet(PacketType.Integer, new IntegerPacketData(key, value)));
    public bool SendString(string key, string value) => SendData(new Packet(PacketType.String, new StringPacketData(key, value)));

    public void OnApplicationQuit() => Client.Dispose();
}


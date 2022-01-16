using System.Collections.Generic;
using System.Net.Sockets;

/**
 * Stores property relevant to the connection instance
 */
public class ServerConnection
{
    public Socket Socket = null;
    public const int BufferSize = 256;
    public byte[] Buffer = new byte[BufferSize];
    public List<byte> Message = new List<byte>();
}

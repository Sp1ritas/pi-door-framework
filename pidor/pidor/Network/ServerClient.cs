using System.Net.Sockets;
using System.Security.Cryptography;
using pidor.Network.Packets;

namespace pidor.Network;

public class ServerClient
{
    public int Id { get; }
    public int Seed { get; set; }
    public TcpClient Client { get; }
    public StreamWriter Writer { get; }
    
    public Aes? SymmetricCipher { get; set; }
    public CryptoStream? CryptoStream { get; set; }
    
    public ServerClient(int id, TcpClient client)
    {
        Id = id;
        Client = client;
        Writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
    }
}
using System.Net.Sockets;
using System.Text;

namespace pidor.Network;

public class ServerClient
{
    public int Id { get; }
    public TcpClient Client { get; }
    public StreamWriter Writer { get; }
    public ServerClient(int id, TcpClient client)
    {
        Id = id;
        Client = client;
        Writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
    }
}
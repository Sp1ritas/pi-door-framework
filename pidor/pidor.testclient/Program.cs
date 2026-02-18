using pidor.Network;

class Program
{
    static async Task Main()
    {
        var client = new TcpSecureClient();
        await client.ConnectAsync("127.0.0.1", 1337);

        Console.ReadLine();
    }
}
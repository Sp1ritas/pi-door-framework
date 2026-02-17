using System.Security.Cryptography;
using pidor.Network;

namespace pidor;

public class Server
{
    public async Task StartAsync(int port)
    {
        if (!Directory.Exists(".pidor"))
            Directory.CreateDirectory(".pidor");

        RSA rsa = null!;

        try
        {
            if (!File.Exists(".pidor/pidor_4096.pub") || !File.Exists(".pidor/pidor_4096.priv8"))
            {
                Console.WriteLine("Initializing server rsa keys");

                rsa = RSA.Create(4096);

                byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                await File.WriteAllBytesAsync(".pidor/pidor_4096.pub", privateKeyBytes);
            }
            else
            {
                byte[] privateKeyBytes = await File.ReadAllBytesAsync(".pidor/pidor_4096.priv8");
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            }

            var server = new TcpServer(rsa);
            await server.StartAsync(port);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
        }
        finally
        {
            rsa?.Dispose();
        }

        Console.ReadLine();
    }
    
    public void Start(int port)
    {
        StartAsync(port).GetAwaiter().GetResult();
    }
}
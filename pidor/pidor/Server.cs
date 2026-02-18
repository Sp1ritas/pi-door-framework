using System.Security.Cryptography;
using pidor.Network;

namespace pidor;

public class Server
{
    private static async Task StartAsync(int port)
    {
        if (!Directory.Exists(".pidor"))
            Directory.CreateDirectory(".pidor");

        RSA? rsa = null!;

        try
        {
            var priv8Path = ".pidor/pidor_4096.priv8";
            var pubPath = ".pidor/pidor_4096.pub8";
            
            if (!File.Exists(priv8Path))
            {
                Console.WriteLine("Initializing server rsa keys");

                rsa = RSA.Create(4096);

                byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

                await File.WriteAllBytesAsync(priv8Path, privateKeyBytes);
                await File.WriteAllBytesAsync(pubPath, publicKeyBytes);
            }
            else
            {
                byte[] privateKeyBytes = await File.ReadAllBytesAsync(priv8Path);

                rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                
                if (!File.Exists(pubPath))
                {
                    byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
                    await File.WriteAllBytesAsync(pubPath, publicKeyBytes);
                }
            }
            
            await new TcpServer(rsa, 50).StartAsync(port);
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
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using pidor.Network.Packets;

namespace pidor.Network;

public class TcpSecureClient
{
    private TcpClient _client = null!;
    private Aes? _aes;
    private readonly ServerUtils _utils = new();

    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);

        Console.WriteLine($"Connected to {host}:{port}");

        using NetworkStream stream = _client.GetStream();

        try
        {
            byte[] rsaPacket = _utils.ReadPacket(stream);
            var authRsa = AuthRsa.FromBytes(rsaPacket);
            using RSA rsa = RSA.Create();

            Console.WriteLine("Received server RSA public key");
            
            byte[] lohas = new byte[4];
            lohas[0] = 0xDE;
            lohas[1] = 0xAD;
            lohas[2] = 0xBE;
            lohas[3] = 0xEF;
            
            var crypted = rsa.Encrypt(lohas, RSAEncryptionPadding.OaepSHA256);
            _utils.WritePacket(stream, crypted);
            Console.WriteLine("Sent some fucking shit");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Handshake failed: {ex.Message}");
        }
    }
}
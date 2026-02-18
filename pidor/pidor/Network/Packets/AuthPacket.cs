using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace pidor.Network.Packets;

public struct AuthRsa
{
    public byte[] PublicKeyBytes { get; set; }

    public AuthRsa(RSA publicKey) => PublicKeyBytes = publicKey.ExportSubjectPublicKeyInfo();

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[1 + 4 + PublicKeyBytes.Length];
        buffer[0] = (byte)PacketType.PacketAuthRSA;
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(PublicKeyBytes.Length)), 0, buffer, 1, 4);
        Array.Copy(PublicKeyBytes, 0, buffer, 5, PublicKeyBytes.Length);
        return buffer;
    }

    public static AuthRsa FromBytes(byte[] data)
    {
        if (data.Length < 5 || data[0] != (byte)PacketType.PacketAuthRSA)
            throw new ArgumentException("Invalid packet type for AuthRsa");
        
        int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 1));
        if (data.Length < 5 + length)
            throw new ArgumentException("Incomplete AuthRsa data");

        RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(new Span<byte>(data, 5, length), out _);
        return new AuthRsa { PublicKeyBytes = rsa.ExportSubjectPublicKeyInfo() };
    }
}

public struct AuthAes
{
    public byte[] Key { get; set; }
    public byte[] IV { get; set; }
    
    public AuthAes(byte[] key, byte[] iv) => (Key, IV) = (key, iv);

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[1 + 4 + Key.Length + 4 + IV.Length];
        int offset = 0;
        buffer[0] = (byte)PacketType.PacketAuthAES;
        offset++;
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Key.Length)), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(Key, 0, buffer, offset, Key.Length);
        offset += Key.Length;
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(IV.Length)), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(IV, 0, buffer, offset, IV.Length);
        return buffer;
    }
    
    public static AuthAes FromBytes(byte[] data)
    {
        int offset = 1;
        int keyLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
        offset += 4;
        byte[] key = new byte[keyLen];
        Array.Copy(data, offset, key, 0, keyLen);
        offset += keyLen;
        int ivLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
        offset += 4;
        byte[] iv = new byte[ivLen];
        Array.Copy(data, offset, iv, 0, ivLen);
        return new AuthAes(key, iv);
    }
}
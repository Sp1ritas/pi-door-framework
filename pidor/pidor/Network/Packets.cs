using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace pidor.Network;

enum PacketType
{
    Dead,
    Auth
}

public class AuthPacket
{
    
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Packet
{
    private PacketType type;
    private string CrcData;
    private byte[] data;
}
namespace pidor.Network.Packets;

public enum PacketType
{
    PacketOK = 1,
    PacketBad = 2,
    // auth bullshit - ONLY SINGLE DIGIT PACKETS
    PacketAuthRSA = 3, 
    PacketAuthAES = 4,
    PacketAuthSeed = 5, // rsa crypted
    // ---
    
    
}
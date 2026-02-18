using System.Net;
using System.Net.Sockets;

namespace pidor.Network;

public class ServerUtils
{
    public byte[] ReadExactBytes(NetworkStream stream, int expectedLength)
    {
        byte[] result = new byte[expectedLength];
        int totalRead = 0;

        while (totalRead < expectedLength)
        {
            int bytesRead = stream.Read(result, totalRead, expectedLength - totalRead);
            if (bytesRead == 0) 
                throw new EndOfStreamException($"[{DateTime.Now:HH:mm:ss}] Connection closed before expected data");
        
            totalRead += bytesRead;
        }
        return result;
    }
    
    //packet struct:
    //  first 4 bytes random shuffled packet enum from the seed
    //  next 4 bytes is the size of the actual content
    //  then the actual content of the packet
    public byte[] ReadPacket(NetworkStream stream)
    {
        byte[] lenghtBuffer = ReadExactBytes(stream, 4);
        int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenghtBuffer, 0));
        byte[] contentBuffer = ReadExactBytes(stream, length);
        
        return contentBuffer;
    }
    
    public void WritePacket(NetworkStream stream, byte[] data)
    {
        byte[] lengthBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
        stream.Write(lengthBuffer, 0, lengthBuffer.Length);
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }
}
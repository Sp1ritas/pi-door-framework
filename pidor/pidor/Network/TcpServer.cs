using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace pidor.Network;

public class TcpServer(RSA ServerRSA)
{
    private TcpListener listener;
    private readonly ConcurrentDictionary<int, ServerClient> clients = new();
    private List<Thread> clientThreads = new();
    private volatile bool running = true;
    private int nextClientId = 1;
    private readonly SemaphoreSlim clientLimit = new(50, 50); // Max 50 clients
    private readonly object lockObj = new object();
    private int activeClients = 0;

    public async Task StartAsync(int port = 1337)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(50);
        
        Console.WriteLine($"TCP Server started on port {port}");
        
        listener.BeginAcceptTcpClient(OnClientConnected, null);
        await Task.Run(PingLoop);
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        if (!running) return;

        try
        {
            if (!clientLimit.Wait(0)) 
            {
                Console.WriteLine("Client rejected - max capacity reached");
                return;
            }

            TcpClient client = listener.EndAcceptTcpClient(ar);
            int clientId = Interlocked.Increment(ref nextClientId);
            
            var serverClient = new ServerClient(clientId, client);
            clients[clientId] = serverClient;
            
            Thread clientThread = new Thread(() => HandleClient(serverClient));
            clientThreads.Add(clientThread);
            clientThread.Start();
            
            lock (lockObj) 
            { 
                activeClients++; 
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client {clientId} connected. Active: {activeClients}");
            }
            
            // Continue accepting
            listener.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Accept error: {ex.Message}");
            clientLimit.Release();
        }
    }

    private void HandleClient(ServerClient serverClient)
    {
        try
        {
            using NetworkStream stream = serverClient.Client.GetStream();
            // rsa key exchange, authentification
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {serverClient.Id} failed to authentificate. error: {ex.Message}");
        }
        finally
        {
            CleanupClient(serverClient.Id);
            clientLimit.Release();
        }
    }

    private void PingLoop()
    {
        while (running)
        {
            // do ping bs 
        }
        
        Stop();
    }

    private void CleanupClient(int clientId)
    {
        clients.TryRemove(clientId, out _);
        lock (lockObj) 
        { 
            activeClients--; 
            Console.WriteLine($"Client {clientId} disconnected. Active: {activeClients}");
        }
    }

    private void Stop()
    {
        Console.WriteLine("Shutting down server...");
        running = false;
        
        listener?.Stop();
        
        foreach (var client in clients.Values)
        {
            client.Client.Close();
        }
        
        clients.Clear();
        
        foreach (var thread in clientThreads)
            thread.Join(1000);
    }
    
    // todo: Seperate utility library for ts shit
    private byte[] ReadExactBytes(NetworkStream stream, int expectedLength)
    {
        byte[] result = new byte[expectedLength];
        int totalRead = 0;

        while (totalRead < expectedLength)
        {
            int bytesRead = stream.Read(result, totalRead, expectedLength - totalRead);
            if (bytesRead == 0) 
                throw new EndOfStreamException("Connection closed before expected data");
        
            totalRead += bytesRead;
        }
        return result;
    }
}


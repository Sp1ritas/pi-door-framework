using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using pidor.Network.Packets;

namespace pidor.Network;

public class TcpServer(RSA ServerRSA, int MaxClients = 50)
{
    private TcpListener listener;
    private readonly ConcurrentDictionary<int, ServerClient> clients = new();
    private List<Thread> clientThreads = new();
    private volatile bool running = true;
    private int nextClientId = 1;
    private readonly SemaphoreSlim clientLimit = new(MaxClients, MaxClients); // Max 50 clients
    private readonly object lockObj = new object();
    private int activeClients = 0;
    
    public async Task StartAsync(int port = 1337)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(50);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] TCP Server started on port {port}");
        
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
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client rejected - max capacity reached");
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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Accept error: {ex.Message}");
            clientLimit.Release();
        }
    }

    private async void HandleClient(ServerClient serverClient)
    {
        try
        {
            using NetworkStream stream = serverClient.Client.GetStream();
            var utils = new ServerUtils();

            // send rsa
            var authRsa = new AuthRsa(ServerRSA);
            utils.WritePacket(stream, authRsa.ToBytes());
            Console.WriteLine("[DEBUG] Sent RSA public key to client");
            
            var encrypted = utils.ReadPacket(stream);
            Console.WriteLine("Received some fucking shit from client, trying to decrypt...");
            var decrypted = ServerRSA.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

            Console.WriteLine($"[DEBUG] Client {serverClient.Id} sent rsa handshake. Decrypted data: {BitConverter.ToString(decrypted)}");
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
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client {clientId} disconnected. Active: {activeClients}");
        }
    }

    private void Stop()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Shutting down server...");
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
}


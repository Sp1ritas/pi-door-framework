using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace pidor.Network;

public class TcpServer
{
    private TcpListener listener;
    private readonly ConcurrentDictionary<int, ServerClient> clients = new();
    private List<Thread> clientThreads = new();
    private volatile bool running = true;
    private int nextClientId = 1;
    private readonly SemaphoreSlim clientLimit = new(50, 50); // Max 50 clients
    private readonly object lockObj = new object();
    private int activeClients = 0;

    public void Start(int port = 1337)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(50);
        
        listener.Server.ReceiveBufferSize = 128 * 1024;
        listener.Server.SendBufferSize = 64 * 1024;
        
        Console.WriteLine($"TCP Server started on port {port}");
        
        listener.BeginAcceptTcpClient(OnClientConnected, null);
        Task.Run(CommandLoop);
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
                Console.WriteLine($"Client {clientId} connected. Active: {activeClients}");
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
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            
            string data;
            while (running && (data = reader.ReadLine()) != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client {serverClient.Id}: {data}");
                
                if (data.Trim().ToLower() == "quit")
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {serverClient.Id} error: {ex.Message}");
        }
        finally
        {
            CleanupClient(serverClient.Id);
            clientLimit.Release();
        }
    }

    public void Broadcast(string message)
    {
        foreach (var kvp in clients)
        {
            try
            {
                kvp.Value.Writer.WriteLine($"[BROADCAST] {message}");
            }
            catch
            {
                // Client disconnected during broadcast
                CleanupClient(kvp.Key);
            }
        }
        Console.WriteLine($"Broadcast sent to {clients.Count} clients");
    }

    private void CommandLoop()
    {
        while (running)
        {
            string input = Console.ReadLine()!;
            if (input?.ToLower() == "quit") break;
            
            if (input!.StartsWith("broadcast "))
            {
                Broadcast(input.Substring(10));
            }
            else if (input == "stats")
            {
                lock (lockObj)
                {
                    Console.WriteLine($"Stats: {activeClients} active, {clients.Count} tracked, {clientThreads.Count} threads");
                }
            }
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

    public void Stop()
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
}


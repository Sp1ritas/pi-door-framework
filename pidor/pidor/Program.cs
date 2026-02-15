using pidor.Network;

var server = new TcpServer();
server.Start(1337);
        
Console.ReadLine(); // keep console open
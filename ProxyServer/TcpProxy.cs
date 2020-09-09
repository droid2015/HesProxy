using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ProxyServer.Client;
using ProxyServer.Models;
using ProxyServer.Server;

namespace ProxyServer
{
    public class TcpProxy: IProxy
    {
        private TcpListener tcpListener;
        public  int MaxPlayers { get; private set; }
        public int Port { get; private set; }
        private int remoteServerPort;
        private string remoteServerIp;
        public  Dictionary<int, DeviceClient> clients = new Dictionary<int, DeviceClient>();
        public void Start(int maxconect,string _remoteServerIp, ushort _remoteServerPort, ushort localPort, string localIp)
        {
            //var clients = new ConcurrentDictionary<IPEndPoint, TcpClient>();
            MaxPlayers = maxconect;
            remoteServerIp = _remoteServerIp;
            remoteServerPort = _remoteServerPort;
            Port = localPort;
            InitializeServerData();
            IPAddress localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
            tcpListener = new System.Net.Sockets.TcpListener(new IPEndPoint(localIpAddress, localPort));
            //server.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            tcpListener.Start();
            Console.WriteLine($"TCP proxy started {localPort} -> {remoteServerIp}|{remoteServerPort}");
            //Bắt đầu hành động nhận kết nối bất đồng bộ
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            /*
            Console.WriteLine($"TCP proxy started {localPort} -> {remoteServerIp}|{remoteServerPort}");
            while (true)
            {

                try
                {
                    var remoteClient = await server.AcceptTcpClientAsync();
                    remoteClient.NoDelay = true;
                    var ips = await Dns.GetHostAddressesAsync(remoteServerIp);
                    //Dua ket noi client vao ds
                    //Chuyen cho hes xu ly

                    for (int i = 1; i <= MaxPlayers; i++)
                    {
                        if (clients[i].tcp.socket == null)
                        {
                            clients[i].tcp.Connect(remoteClient);
                            new HesClient(remoteClient, new IPEndPoint(ips.First(), remoteServerPort));
                        }
                    }
         
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }

            }*/
        }
        private  void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    TcpClient clientTransfer = new TcpClient(remoteServerIp, remoteServerPort);                        
                    clients[i].tcp.Connect(_client,clientTransfer);                    
                    //new HesClient(_client, new IPEndPoint(IPAddress.Parse(remoteServerIp), remoteServerPort));
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }
       
        /// <summary>
        /// Khoi tao Deviceclient
        /// </summary>
        private  void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new DeviceClient(i));
            }
        }
    }
    

}

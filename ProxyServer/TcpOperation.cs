using ProxyServer.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer
{
    public class TcpOperation
    {
        private TcpListener tcpListener;
        public int Port { get; private set; }
        public string Ip { get; private set; }
        public int MaxConn { get; private set; }
        public Dictionary<int, OpClient> clients = new Dictionary<int, OpClient>();
        public void Start(int maxconect, int localPort, string localIp)
        {
            
            MaxConn = maxconect;            
            Port = localPort;
            Ip = localIp;
            InitializeServerData();
            IPAddress localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
            tcpListener = new System.Net.Sockets.TcpListener(new IPEndPoint(localIpAddress, localPort));            
            tcpListener.Start();
            Console.WriteLine($"TCP operation started {localPort}");
            //Bắt đầu hành động nhận kết nối bất đồng bộ
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);           
        }
        private void InitializeServerData()
        {
            for (int i = 1; i <= MaxConn; i++)
            {
                clients.Add(i, new OpClient(i));
            }
        }
        private void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxConn; i++)
            {
                if (clients[i].tcp.socket == null)
                {                   
                    clients[i].tcp.Connect(_client);                    
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }
    }
}

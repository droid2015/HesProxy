using ProxyServer.Client;
using ProxyServer.Server;
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
        public static int MaxConn { get; private set; }
        public static Dictionary<int, OpClient> clients = new Dictionary<int, OpClient>();

        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
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
            //Khoi tạo
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, WelcomeReceived },
                { (int)ClientPackets.docModem, DocModem },
            };
        }
        public  void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            clients[_fromClient].SendIntoVanHanh(_username);
        }

        public  void DocModem(int _fromClient, Packet _packet)
        {
            //bool[] _inputs = new bool[_packet.ReadInt()];
            //for (int i = 0; i < _inputs.Length; i++)
            //{
            //    _inputs[i] = _packet.ReadBool();
            //}
            //Quaternion _rotation = _packet.ReadQuaternion();

            //clients[_fromClient].user.SetInput(_inputs, _rotation);
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

using System;
using System.Collections.Concurrent;
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
    public class TcpProxy : IProxy
    {
        private TcpListener tcpListener;
        private TcpClient hesServer;
        private TcpClient opServer;
        public int MaxPlayers { get; private set; }
        public int Port { get; private set; }
        private int remoteServerPort;
        private string remoteServerIp;
        //Van hanh
        private string opIp;
        private int opPort;

        private bool isConnected;
        public static Dictionary<int, DeviceClient> clients = new Dictionary<int, DeviceClient>();
        public static ConcurrentDictionary<string, int> imei = new ConcurrentDictionary<string, int>();
        //
        public delegate void ImeiHandler(int _fromClient, string imei);
        public static ImeiHandler handler;
        public void Start(int maxconect, string _remoteServerIp, ushort _remoteServerPort, ushort localPort, string localIp,string _opIp,int _opPort)
        {
            MaxPlayers = maxconect;
            remoteServerIp = _remoteServerIp;
            remoteServerPort = _remoteServerPort;
            Port = localPort;
            opIp = _opIp;
            opPort = _opPort;
            InitializeServerData();
            IPAddress localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
            tcpListener = new System.Net.Sockets.TcpListener(new IPEndPoint(localIpAddress, localPort));
            //server.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            tcpListener.Start();
            Console.WriteLine($"TCP proxy started {localPort} -> {remoteServerIp}|{remoteServerPort}");
            //Bắt đầu hành động nhận kết nối bất đồng bộ từ thiết bị
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            //Kiem tra ket noi den HES server
            hesServer = new TcpClient();
            hesServer.BeginConnect(IPAddress.Parse(remoteServerIp), remoteServerPort, ConnectCallback, null);
            //Kết nối đến opServer
            opServer = new TcpClient(opIp, opPort);
            //Đăng ký handler
            handler = new ImeiHandler(XulyImei);
            //khoi tao
            //imei.TryAdd("123456", 1);
        }
        public void XulyImei(int _fromClient, string _imei)
        {
            Console.WriteLine("gia tri {_imei} {_fromclient}");
            imei.AddOrUpdate(_imei, _fromClient, ( _fromclient, oldvalue) => _fromClient);
        }
        private void ConnectCallback(IAsyncResult _result)
        {
            try
            {
                //Kiểm tra HES server có connect
                hesServer.EndConnect(_result);
                hesServer.NoDelay = true;
                if (!hesServer.Connected)
                {
                    isConnected = false;
                    return;
                }

                isConnected = true;
            }
            catch (Exception ex)
            {
                isConnected = false;
            }
        }
        /// <summary>
        /// Thiết bị kết nối đến Proxy
        /// </summary>
        /// <param name="_result"></param>
        private void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            _client.NoDelay = true;
            //Bắt đầu nhận kết nối thiết bị mới
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            //Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                //Kiểm tra slot còn trống
                if (clients[i].tcp.socket == null)
                {
                    //Nếu Hes server có kết nối
                    if (isConnected)
                    {
                        TcpClient clientTransfer = new TcpClient(remoteServerIp, remoteServerPort);
                        clients[i].tcp.Connect(_client, clientTransfer,opServer);
                        Console.WriteLine("id:" + i);
                        return;
                    }
                    //Hes server chưa kết nối
                    else
                    {
                        clients[i].tcp.Connect(_client, null,opServer);
                        return;
                    }

                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }


        /// <summary>
        /// Khoi tao Deviceclient
        /// </summary>
        private void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new DeviceClient(i));
            }
        }
    }


}

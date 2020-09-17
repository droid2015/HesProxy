using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.Client;
using ProxyServer.Models;
using ProxyServer.Modem;
using ProxyServer.Server;
using static ProxyServer.TcpOperation;

namespace ProxyServer
{
    public class TcpProxy : IProxy
    {
        private TcpListener tcpListener;
        private TcpClient hesServer;        
        public int MaxPlayers { get; private set; }
        public int Port { get; private set; }
        private int remoteServerPort;
        private string remoteServerIp;       

        private bool isConnected=true;
        public Dictionary<int, DeviceClient> clients = new Dictionary<int, DeviceClient>();
        public ConcurrentDictionary<string, int> imei = new ConcurrentDictionary<string, int>();
        //
        public delegate void ImeiHandler(int _fromClient, string imei);
        //Xử lý package
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public Dictionary<int, PacketHandler> packetHandlers;
        //
        public bool waitModem = false;
        //
        public int opId;
        public ImeiHandler handler;
        public void Start(int maxconect, string _remoteServerIp, ushort _remoteServerPort, ushort localPort, string localIp)
        {
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
            //Bắt đầu hành động nhận kết nối bất đồng bộ từ thiết bị
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            //Kiem tra ket noi den HES server
            //hesServer = new TcpClient(remoteServerIp, remoteServerPort);
            //hesServer.BeginConnect(IPAddress.Parse(remoteServerIp), remoteServerPort, ConnectCallback, null);            
            //Đăng ký handler
            handler = new ImeiHandler(XulyImei);
            //khoi tao
            //imei.TryAdd("123456", 1);
        }
        public void XulyImei(int _fromClient, string _imei)
        {
            Console.WriteLine($"gia tri {_imei} {_fromClient}");
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
                        clients[i].tcp.Connect(_client, clientTransfer);

                        Console.WriteLine("id:" + i);
                        return;
                    }
                    //Hes server chưa kết nối
                    else
                    {
                        clients[i].tcp.Connect(_client, null);
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
                clients.Add(i, new DeviceClient(i,this));
            }
            //Khoi tạo
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, WelcomeReceived },
                { (int)ClientPackets.connect3, Connect3Received },
                { (int)ClientPackets.docModem, DocModem },
                { (int)ClientPackets.traKQModem, TraKetQuaModem },
            };
        }
        #region Server Send
        private void SendTCPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            //package dinh dang lai chieudai idpackage noidung clientid
            clients[_toClient].tcp.SendData(_packet);
        }

        /// <summary>Sends a packet to all clients via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        private void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= TcpOperation.MaxConn; i++)
            {
                clients[i].tcp.SendData(_packet);
            }
        }
        /// <summary>Sends a packet to all clients except one via TCP.</summary>
        /// <param name="_exceptClient">The client to NOT send the data to.</param>
        /// <param name="_packet">The packet to send.</param>
        private void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= TcpOperation.MaxConn; i++)
            {
                if (i != _exceptClient)
                {
                    clients[i].tcp.SendData(_packet);
                }
            }
        }
        public void Welcome(int _toClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                //package có định dạng idpackage noidung idclient
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }
        public void Connect3(int _toClient, NguoiVanHanh _user)
        {
            using (Packet _packet = new Packet((int)ServerPackets.connect3))
            {

                _packet.Write(_user.id);
                _packet.Write(_user.username);
                _packet.Write(_user.lat);
                _packet.Write(_user.lon);
                _packet.Write("CONNECT3");
                _packet.Write(_toClient);
                SendTCPData(_toClient, _packet);
            }
        }
        #endregion
        public void Connect3Received(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            //clients[_fromClient].SendIntoVanHanh(_username);
        }
        public void TraKetQuaModem(int _fromClient, Packet _packet)
        {
            int idpack = _packet.ReadInt();
            int len = _packet.UnreadLength();
            byte[] _data = _packet.ReadBytes(len,true);
            //int _clientIdCheck = _packet.ReadInt();

            Console.WriteLine($"{clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            //if (_fromClient != _clientIdCheck)
            //{
            //    Console.WriteLine($"Player \"{_data}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            //}
            clients[_fromClient].tcp.SendData(_data);
        }
        public void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            clients[_fromClient].Connect3(_username);
        }

        public void DocModem(int _fromClient, Packet _packet)
        {
            opId = _packet.ReadInt();
            string _imei = _packet.ReadString();

            Console.WriteLine($"{clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != opId)
            {
                Console.WriteLine($"Imei \"{_imei}\" (ID: {_fromClient}) has assumed the wrong client ID ({opId})!");
            }
            //Kiem tra doc trong thread;
            ThreadManager.ExecuteOnMainThread(() =>
            {
                int modemid = imei[_imei];
                //
                string docmodem = UtilityModem.encrypt(UtilityModem.DOCMODEM);
                clients[modemid].tcp.SendData(Encoding.ASCII.GetBytes(docmodem));
                //Đọc kết quả
                //Packet packet = new Packet(4);
                //clients[modemid].tcp.ReadData(_fromClient,packet);
                waitModem = true;
            });
            //

        }
    }


}

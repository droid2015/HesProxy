using ProxyServer.Modem;
using ProxyServer.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer.Client
{
    //doc https://github.com/tom-weiland/tcp-udp-networking
    //doc http://jamesslocum.com/post/67566023889
    //https://stackoverflow.com/questions/51077233/using-socket-in-flutter-apps

    public class DeviceClient
    {

        public static int dataBufferSize = 4096;

        public int id;
        public TCP tcp;
        private TcpProxy proxy;
        public NguoiVanHanh user;
        public DeviceClient(int _clientId, TcpProxy _proxy)
        {
            id = _clientId;
            proxy = _proxy;
            tcp = new TCP(id, this);
        }
        public void Connect3(string _userName)
        {
            user = new NguoiVanHanh(id, _userName, 0, 0);

            // Send all users to the new user
            foreach (DeviceClient _client in proxy.clients.Values)
            {
                if (_client.user != null)
                {
                    if (_client.id != id)
                    {
                        proxy.Connect3(id, user);
                    }
                }
            }

            // Send the new player to all players (including himself)
            foreach (OpClient _client in TcpOperation.clients.Values)
            {
                if (_client.user != null)
                {
                    proxy.Connect3(id, user);
                }
            }
        }

        public class TCP
        {
            private DeviceClient connection;
            public TcpClient socket;
            public TcpClient socketTransfer;

            private readonly int id;
            private NetworkStream stream;
            private NetworkStream streamTransfer;
            private byte[] receiveBuffer;
            private byte[] receiveTransferBuffer;
            private Packet receivedData;//package


            public TCP(int _id, DeviceClient _connnection)
            {
                id = _id;
                connection = _connnection;
            }

            public void Connect(TcpClient _socket, TcpClient _socketTransfer)
            {
                socket = _socket;

                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;
                //Khai báo package để giao tiếp client
                receivedData = new Packet();

                stream = socket.GetStream();

                receiveBuffer = new byte[dataBufferSize];
                receiveTransferBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                if (_socketTransfer != null)
                {
                    socketTransfer = _socketTransfer;
                    streamTransfer = socketTransfer.GetStream();
                    socketTransfer.ReceiveBufferSize = dataBufferSize;
                    socketTransfer.SendBufferSize = dataBufferSize;
                    //Gởi gói tin 
                    //string doc = UtilityModem.encrypt(UtilityModem.DOCMODEM);
                    //stream.BeginWrite(Encoding.ASCII.GetBytes(doc), 0, doc.Length, SendCallback, null);
                    streamTransfer.BeginRead(receiveTransferBuffer, 0, dataBufferSize, ReceiveTransferCallback, null);
                }
                //Server gởi gói tin
                connection.proxy.Welcome(id, "Welcome to the server!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // Send data to appropriate client
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }
            public void SendData(byte[] dat)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(dat, 0, dat.Length, null, null); // Send data to appropriate client
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }
            public void ReadData(int _client,Packet packet)
            {
                try
                {
                    if (socket != null)
                    {

                        byte[] buffer=new byte[dataBufferSize];
                        stream.Read(buffer, 0, dataBufferSize); 
                        
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }
            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains a packet
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            connection.proxy.packetHandlers[_packetId](id, _packet); // Call appropriate method to handle the packet
                        }
                    });

                    _packetLength = 0; // Reset packet length
                    if (receivedData.UnreadLength() >= 4)
                    {
                        // If client's received data contains another packet
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0)
                        {
                            // If packet contains no data
                            return true; // Reset receivedData instance to allow it to be reused
                        }
                    }
                }

                if (_packetLength <= 1)
                {
                    return true; // Reset receivedData instance to allow it to be reused
                }

                return false;
            }
            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        connection.Disconnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);
                    Console.WriteLine(string.Format("nhan tu modem {0} {1}", socket.Client.RemoteEndPoint, Encoding.ASCII.GetString(_data)));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                    //Chuyển data sang transfer
                    if (streamTransfer != null)
                        streamTransfer.BeginWrite(_data, 0, _data.Length, null, null);
                    // TODO: handle data 
                    
                    //Xử lý dữ liệu
                    //Nếu data là package
                    if (HandleData(_data))
                    {
                        receivedData.Reset(true);
                    }
                    else
                    {
                        receivedData.Reset(false);
                        if (connection.proxy.waitModem)
                        {
                            ThreadManager.ExecuteOnMainThread(() =>
                            {
                                using (Packet _packet = new Packet((int)ClientPackets.traKQModem))
                                {
                                    //_packet.Write(_byteLength);
                                    _packet.SetBytes(_data);
                                    //_packet.WriteLength();
                                    //_packet.Write(_byteLength);
                                    //_packet.Write(_data);
                                    //_packet.Write(id);                                    
                                    connection.proxy.packetHandlers[(int)ClientPackets.traKQModem](connection.proxy.opId, _packet); // Call appropriate method to handle the packet
                                }
                            });
                            connection.proxy.waitModem = false;
                        }
                        else
                        {
                            //check chứa imei
                            string imeistr = Encoding.ASCII.GetString(_data);
                            int index = imeistr.IndexOf("+CSQ");
                            if (index >= 16)
                            {
                                string imei = imeistr.Substring(index - 16, 16);
                                //Thực hiện trong mainthread, cap nhat tcpip
                                ThreadManager.ExecuteOnMainThread(() =>
                                {
                                    Console.WriteLine("imei:" + imei + " id:" + id);
                                    connection.proxy.handler(id, imei); // Call appropriate method to handle the packet                            
                                });
                            }
                        }
                        
                        
                    }
                   
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    //connection.Disconnect();
                    // TODO: disconnect
                }
            }
            private void ReceiveTransferCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = streamTransfer.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        connection.Disconnect();
                        // TODO: disconnect
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveTransferBuffer, _data, _byteLength);
                    Console.WriteLine(string.Format("nhan tu evnhes {0} {1}", socket.Client.RemoteEndPoint, Encoding.ASCII.GetString(_data)));
                    stream.BeginWrite(_data, 0, _data.Length, null, null);
                    streamTransfer.BeginRead(receiveTransferBuffer, 0, dataBufferSize, ReceiveTransferCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP transfer data: {_ex}");
                    //connection.Disconnect();
                    // TODO: disconnect
                }
            }

            public void Disconnect()
            {
                socket.Close();

                if (socketTransfer != null)
                {
                    socketTransfer.Close();
                    streamTransfer = null;
                    socketTransfer = null;

                }
                stream = null;
                receiveBuffer = null;
                receiveTransferBuffer = null;
                socket = null;
            }
        }
        private void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
            tcp.Disconnect();
        }
    }
}

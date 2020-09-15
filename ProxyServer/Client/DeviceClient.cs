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

        public DeviceClient(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id, this);
        }

        public class TCP
        {
            private DeviceClient connection;
            public TcpClient socket;
            public TcpClient socketTransfer;
            public TcpClient opsocket;
            private readonly int id;
            private NetworkStream stream;
            private NetworkStream streamTransfer;
            private NetworkStream streamOperation;
            private byte[] receiveBuffer;
            private byte[] receiveTransferBuffer;
            private byte[] opBuffer;//buffer của vận hành

            public TCP(int _id, DeviceClient _connnection)
            {
                id = _id;
                connection = _connnection;
            }

            public void Connect(TcpClient _socket, TcpClient _socketTransfer,TcpClient _opSocket)
            {
                socket = _socket;
                opsocket = _opSocket;

                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                opsocket.ReceiveBufferSize = dataBufferSize;
                opsocket.SendBufferSize = dataBufferSize;
                stream = socket.GetStream();
                streamOperation = opsocket.GetStream();

                receiveBuffer = new byte[dataBufferSize];
                receiveTransferBuffer = new byte[dataBufferSize];
                opBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                if (_socketTransfer != null)
                {
                    socketTransfer = _socketTransfer;
                    streamTransfer = socketTransfer.GetStream();
                    _socketTransfer.ReceiveBufferSize = dataBufferSize;
                    _socketTransfer.SendBufferSize = dataBufferSize;
                    //Gởi gói tin 
                    //string doc = UtilityModem.encrypt(UtilityModem.DOCMODEM);
                    //stream.BeginWrite(Encoding.ASCII.GetBytes(doc), 0, doc.Length, SendCallback, null);
                    streamTransfer.BeginRead(receiveTransferBuffer, 0, dataBufferSize, ReceiveTransferCallback, null);
                    // TODO: send welcome packet
                }
                streamOperation.BeginRead(opBuffer, 0, dataBufferSize, ReciveOpCallback, null);
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
            private void ReciveOpCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = streamOperation.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        connection.Disconnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy(opBuffer, _data, _byteLength);//copy tu buffer stream sang data[]
                    stream.BeginWrite(_data, 0, _data.Length, null, null);//ghi vào modem 
                    streamOperation.BeginRead(opBuffer, 0, dataBufferSize, ReciveOpCallback, null);//đọc tiếp
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    connection.Disconnect();
                    // TODO: disconnect
                }
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
                    //check chứa imei
                    string imeistr = Encoding.ASCII.GetString(_data);
                    int index = imeistr.IndexOf("CSQ+");
                    if (index>16)
                    {
                        string imei = imeistr.Substring(index - 16, 16);
                        //Thực hiện trong mainthread, cap nhat tcpip
                        ThreadManager.ExecuteOnMainThread(() =>
                        {
                            Console.WriteLine("imei:" + imei + " id:" + id);
                            TcpProxy.handler(id, imei); // Call appropriate method to handle the packet                            
                        });
                    }
                    //Console.WriteLine(string.Format("nhan tu modem {0} {1}", socket.Client.RemoteEndPoint, Encoding.ASCII.GetString(_data)));
                    //Chuyển data sang transfer
                    if (streamTransfer != null)
                        streamTransfer.BeginWrite(_data, 0, _data.Length, null, null);
                    // TODO: handle data
                    streamOperation.BeginWrite(_data, 0, _data.Length, null, null);

                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    connection.Disconnect();
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
                    //Console.WriteLine(string.Format("nhan tu evnhes {0} {1}", socket.Client.RemoteEndPoint, Encoding.ASCII.GetString(_data)));
                    stream.BeginWrite(_data, 0, _data.Length, null, null);
                    // TODO: handle data
                    if (streamTransfer != null)
                        streamTransfer.BeginRead(receiveTransferBuffer, 0, dataBufferSize, ReceiveTransferCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP transfer data: {_ex}");
                    connection.Disconnect();
                    // TODO: disconnect
                }
            }

            public void Disconnect()
            {
                socket.Close();
                if (socketTransfer != null)
                {
                    socketTransfer.Close();
                    socketTransfer = null;
                    streamTransfer = null;
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

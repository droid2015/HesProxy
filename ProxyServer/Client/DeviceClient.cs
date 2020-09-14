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
            private readonly int id;
            private NetworkStream stream;
            private NetworkStream streamTransfer;
            private byte[] receiveBuffer;
            private byte[] receiveTransferBuffer;

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

                stream = socket.GetStream();
                receiveBuffer = new byte[dataBufferSize];
                receiveTransferBuffer = new byte[dataBufferSize];
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
                    //Chuyển data sang transfer
                    if (streamTransfer != null)
                        streamTransfer.BeginWrite(_data, 0, _data.Length, null, null);
                    // TODO: handle data
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
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
                        // TODO: disconnect
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveTransferBuffer, _data, _byteLength);
                    Console.WriteLine(string.Format("nhan tu evnhes {0} {1}", socket.Client.RemoteEndPoint, Encoding.ASCII.GetString(_data)));
                    stream.BeginWrite(_data, 0, _data.Length, null, null);
                    // TODO: handle data
                    if (streamTransfer != null)
                        streamTransfer.BeginRead(receiveTransferBuffer, 0, dataBufferSize, ReceiveTransferCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP transfer data: {_ex}");
                    // TODO: disconnect
                }
            }

            public void Disconnect()
            {
                socket.Close();
                if(socketTransfer!=null)
                socketTransfer.Close();
                stream = null;
                streamTransfer = null;
                receiveBuffer = null;
                receiveTransferBuffer = null;
                socket = null;
                socketTransfer = null;
            }
        }
        private void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
            tcp.Disconnect();
        }
    }
}

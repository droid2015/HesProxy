using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ProxyServer.Server;

namespace ProxyServer.Client
{
    public class OpClient
    {
        public static int dataBufferSize = 4096;

        public int id;
        public TCPOperation tcp;
        public NguoiVanHanh user;
        public OpClient(int _clientId)
        {
            id = _clientId;
            tcp = new TCPOperation(id, this);
        }
        private void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
            tcp.Disconnect();
        }
        public void Connect3(string _userName)
        {
            user = new NguoiVanHanh(id, _userName, 0,0);

            // Send all users to the new user
            foreach (OpClient _client in TcpOperation.clients.Values)
            {
                if (_client.user != null)
                {
                    if (_client.id != id)
                    {
                        ServerSend.Connect3(id,user);
                    }
                }
            }

            // Send the new player to all players (including himself)
            foreach (OpClient _client in TcpOperation.clients.Values)
            {
                if (_client.user != null)
                {
                    ServerSend.Connect3(id, user);
                }
            }
        }
        public class TCPOperation
        {

            private OpClient connection;
            public TcpClient socket;            
            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;
            private byte[] receiveTransferBuffer;
            public TCPOperation(int _id, OpClient _connnection)
            {
                id = _id;
                connection = _connnection;
            }
            public void Connect(TcpClient _socket)
            {
                socket = _socket;                
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;                
                stream = socket.GetStream();
                //Khai báo package để giao tiếp client
                receivedData = new Packet();

                receiveBuffer = new byte[dataBufferSize];
                receiveTransferBuffer = new byte[dataBufferSize];
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                //Gởi gói tin
                //ServerSend.Connect3(id, "CONNECT3");
                ServerSend.Welcome(id, "Welcome to the server!");
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
                    //Xử lý data
                    receivedData.Reset(HandleData(_data));
                    // TODO: handle data
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    connection.Disconnect();
                    // TODO: disconnect
                }
            }
            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        Console.WriteLine(@"send client "+Encoding.ASCII.GetString(_packet.ToArray()));
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // Send data to appropriate client
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
                            TcpOperation.packetHandlers[_packetId](id, _packet); // Call appropriate method to handle the packet
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
            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receiveBuffer = null;
                socket = null;
            }
        }
    }
}

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
        public void SendIntoVanHanh(string _userName)
        {
            user = new NguoiVanHanh(id, _userName, 0,0);

            // Send all users to the new user
            foreach (OpClient _client in TcpOperation.clients.Values)
            {
                if (_client.user != null)
                {
                    if (_client.id != id)
                    {
                        ServerSend.SpawnPlayer(id, _client.user);
                    }
                }
            }

            // Send the new player to all players (including himself)
            foreach (OpClient _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    ServerSend.SpawnPlayer(_client.id, player);
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

                receiveBuffer = new byte[dataBufferSize];
                receiveTransferBuffer = new byte[dataBufferSize];
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);               
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
                    //Chuyển data sang transfer                   
                    // TODO: handle data
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    // TODO: disconnect
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
                            Server.packetHandlers[_packetId](id, _packet); // Call appropriate method to handle the packet
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

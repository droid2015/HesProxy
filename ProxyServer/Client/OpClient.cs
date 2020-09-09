using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer.Client
{
    public class OpClient
    {
        public static int dataBufferSize = 4096;

        public int id;
        public TCPOperation tcp;
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
        public class TCPOperation
        {
            private OpClient connection;
            public TcpClient socket;            
            private readonly int id;
            private NetworkStream stream;            
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

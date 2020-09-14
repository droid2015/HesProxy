using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

namespace DeviceClient.Simulator
{
    public class Client
    {       
        public static int dataBufferSize = 4096;

        public string ip = "127.0.0.1";
        public int port = 9192;
        public int myId = 0;
        public TCP tcp;  

        private bool isConnected = false;
        private delegate void PacketHandler(Packet _packet);
        private static Dictionary<int, PacketHandler> packetHandlers;
        public Client()
        {

        }    

       
        /// <summary>Attempts to connect to the server.</summary>
        public void ConnectToServer()
        {
            tcp = new TCP();
            

            InitializeClientData();

            isConnected = true;
            tcp.Connect(this); // Connect tcp, udp gets connected once tcp is done
        }

        public class TCP
        {
            public TcpClient socket;
            private Client connection;

            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            /// <summary>Attempts to connect to the server via TCP.</summary>
            public void Connect(Client _client)
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };
                connection = _client;

                receiveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(connection.ip, connection.port, ConnectCallback, socket);
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            private void ConnectCallback(IAsyncResult _result)
            {
                socket.EndConnect(_result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            /// <summary>Sends data to the client via TCP.</summary>
            /// <param name="_packet">The packet to send.</param>
            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // Send data to server
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to server via TCP: {_ex}");
                }
            }

            /// <summary>Reads incoming data from the stream.</summary>
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

                    receivedData.Reset(HandleData(_data)); // Reset receivedData if all data was handled
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="_data">The recieved data.</param>
            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;
                //1. copy vào package
                receivedData.SetBytes(_data);
                //2. kiểm tra gói tin
                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains a packet
                    _packetLength = receivedData.ReadInt(true);
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
                    Console.WriteLine(Encoding.ASCII.GetString(_packetBytes));
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            Console.WriteLine(_packetId);
                            packetHandlers[_packetId](_packet); // Call appropriate method to handle the packet
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

            /// <summary>Disconnects from the server and cleans up the TCP connection.</summary>
            private void Disconnect()
            {
                connection.Disconnect();

                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }
        public static void Welcome(Packet _packet)
        {
            string _msg = _packet.ReadString();
            int _myId = _packet.ReadInt();

            Console.WriteLine($"Message from server: {_msg}");

        }

        public static void SpawnPlayer(Packet _packet)
        {
           
        }

        public static void PlayerPosition(Packet _packet)
        {
          
        }

        public static void PlayerRotation(Packet _packet)
        {
           
        }

        public static void PlayerDisconnected(Packet _packet)
        {
        }

        public static void PlayerHealth(Packet _packet)
        {
            
        }

        public static void PlayerRespawned(Packet _packet)
        {
          
        }

        public static void CreateItemSpawner(Packet _packet)
        {
           
        }

        public static void ItemSpawned(Packet _packet)
        {
            int _spawnerId = _packet.ReadInt();

          
        }

        public static void ItemPickedUp(Packet _packet)
        {
            int _spawnerId = _packet.ReadInt();
            int _byPlayer = _packet.ReadInt();

            
        }

        public static void SpawnProjectile(Packet _packet)
        {
            int _projectileId = _packet.ReadInt();
          
        }

        public static void ProjectilePosition(Packet _packet)
        {
            int _projectileId = _packet.ReadInt();
          
        }

        public static void ProjectileExploded(Packet _packet)
        {
            int _projectileId = _packet.ReadInt();
           
        }

        public static void SpawnEnemy(Packet _packet)
        {
            int _enemyId = _packet.ReadInt();
          
        }

        public static void EnemyPosition(Packet _packet)
        {
            int _enemyId = _packet.ReadInt();
          
        }

        public static void EnemyHealth(Packet _packet)
        {
            int _enemyId = _packet.ReadInt();
            float _health = _packet.ReadFloat();

        }
        /// <summary>Initializes all necessary client data.</summary>
        private void InitializeClientData()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, Welcome },
            { (int)ServerPackets.spawnPlayer, SpawnPlayer },
            { (int)ServerPackets.playerPosition, PlayerPosition },
            { (int)ServerPackets.playerRotation, PlayerRotation },
            { (int)ServerPackets.playerDisconnected, PlayerDisconnected },
            { (int)ServerPackets.playerHealth, PlayerHealth },
            { (int)ServerPackets.playerRespawned, PlayerRespawned },
            { (int)ServerPackets.createItemSpawner,CreateItemSpawner },
            { (int)ServerPackets.itemSpawned, ItemSpawned },
            { (int)ServerPackets.itemPickedUp, ItemPickedUp },
            { (int)ServerPackets.spawnProjectile, SpawnProjectile },
            { (int)ServerPackets.projectilePosition, ProjectilePosition },
            { (int)ServerPackets.projectileExploded, ProjectileExploded },
            { (int)ServerPackets.spawnEnemy, SpawnEnemy },
            { (int)ServerPackets.enemyPosition,EnemyPosition },
            { (int)ServerPackets.enemyHealth, EnemyHealth },
        };
            Console.WriteLine("Initialized packets.");
        }

        /// <summary>Disconnects from the server and stops all network traffic.</summary>
        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                tcp.socket.Close();                

                Console.WriteLine("Disconnected from server.");
            }
        }
    }

}
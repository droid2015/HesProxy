using ProxyServer.Client;
using System;
namespace ProxyServer.Server
{
    class ServerSend
    {
        /// <summary>Sends a packet to a client via TCP.</summary>
        /// <param name="_toClient">The client to send the packet the packet to.</param>
        /// <param name="_packet">The packet to send to the client.</param>
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            //package dinh dang lai chieudai idpackage noidung clientid
            TcpOperation.clients[_toClient].tcp.SendData(_packet);
        }

        /// <summary>Sends a packet to all clients via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        private static void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= TcpOperation.MaxConn; i++)
            {
                TcpOperation.clients[i].tcp.SendData(_packet);
            }
        }
        /// <summary>Sends a packet to all clients except one via TCP.</summary>
        /// <param name="_exceptClient">The client to NOT send the data to.</param>
        /// <param name="_packet">The packet to send.</param>
        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= TcpOperation.MaxConn; i++)
            {
                if (i != _exceptClient)
                {
                    TcpOperation.clients[i].tcp.SendData(_packet);
                }
            }
        }       

        #region Packets
        /// <summary>Sends a welcome message to the given client.</summary>
        /// <param name="_toClient">The client to send the packet to.</param>
        /// <param name="_msg">The message to send.</param>
        public static void Welcome(int _toClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                //package có định dạng idpackage noidung idclient
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }
        public static void Connect3(int _toClient, NguoiVanHanh _user)
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

        /// <summary>Tells a client to spawn a player.</summary>
        /// <param name="_toClient">The client that should spawn the player.</param>
        /// <param name="_player">The player to spawn.</param>
        public static void SpawnPlayer(int _toClient, NguoiVanHanh _user)
        {
            using (Packet _packet = new Packet((int)ServerPackets.docModem))
            {
                _packet.Write(_user.id);
                _packet.Write(_user.username);
                _packet.Write(_user.lat);
                _packet.Write(_user.lon);

                SendTCPData(_toClient, _packet);
            }
        }



        #endregion
    }
}

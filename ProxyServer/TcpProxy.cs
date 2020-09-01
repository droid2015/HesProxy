using System;
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
    public class TcpProxy: IProxy
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, DeviceClient> clients = new Dictionary<int, DeviceClient>();
        public async Task Start(int maxconect,string remoteServerIp, ushort remoteServerPort, ushort localPort, string localIp)
        {
            //var clients = new ConcurrentDictionary<IPEndPoint, TcpClient>();
            MaxPlayers = maxconect;
            InitializeServerData();
            IPAddress localIpAddress = string.IsNullOrEmpty(localIp) ? IPAddress.IPv6Any : IPAddress.Parse(localIp);
            var server = new System.Net.Sockets.TcpListener(new IPEndPoint(localIpAddress, localPort));
            //server.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            server.Start();

            Console.WriteLine($"TCP proxy started {localPort} -> {remoteServerIp}|{remoteServerPort}");
            while (true)
            {

                try
                {
                    var remoteClient = await server.AcceptTcpClientAsync();
                    remoteClient.NoDelay = true;
                    var ips = await Dns.GetHostAddressesAsync(remoteServerIp);
                    //Dua ket noi client vao ds
                    //Chuyen cho hes xu ly

                    for (int i = 1; i <= MaxPlayers; i++)
                    {
                        if (clients[i].tcp.socket == null)
                        {
                            clients[i].tcp.Connect(remoteClient);
                            new HesClient(remoteClient, new IPEndPoint(ips.First(), remoteServerPort));
                        }
                    }
         
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }

            }
        }
        /// <summary>
        /// Khoi tao Deviceclient
        /// </summary>
        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new DeviceClient(i));
            }
        }
    }
    

}

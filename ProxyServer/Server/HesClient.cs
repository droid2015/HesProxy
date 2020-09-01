﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace ProxyServer.Server
{
    public class HesClient
    {
        private System.Net.Sockets.TcpClient _remoteClient;
        private IPEndPoint _clientEndpoint;
        private IPEndPoint _remoteServer;

        public HesClient(System.Net.Sockets.TcpClient remoteClient, IPEndPoint remoteServer)
        {
            _remoteClient = remoteClient;


            _remoteServer = remoteServer;
            client.NoDelay = true;
            _clientEndpoint = (IPEndPoint)_remoteClient.Client.RemoteEndPoint;
            Console.WriteLine($"Established {_clientEndpoint} => {remoteServer}");
            Run();
        }


        public System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();



        private void Run()
        {

            Task.Run(async () =>
            {
                try
                {
                    using (_remoteClient)
                    using (client)
                    {
                        await client.ConnectAsync(_remoteServer.Address, _remoteServer.Port);
                        var serverStream = client.GetStream();
                        var remoteStream = _remoteClient.GetStream();

                        await Task.WhenAny(remoteStream.CopyToAsync(serverStream), serverStream.CopyToAsync(remoteStream));



                    }
                }
                catch (Exception) { }
                finally
                {
                    Console.WriteLine($"Closed {_clientEndpoint} => {_remoteServer}");
                    _remoteClient = null;
                }
            });
        }


    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProxyServer.Models;
using ProxyServer.Server;

namespace ProxyServer
{
    class Program
    {
        private static bool isRunning = false;
        private static TcpOperation op;
        private static Dictionary<int, TcpProxy> ports;
        static void  Main(string[] args)
        {
            try
            {
                var configJson = System.IO.File.ReadAllText("config.json");
                var configs = JsonSerializer.Deserialize<Vanhanh>(configJson);
                isRunning = true;
                Thread mainThread = new Thread(new ThreadStart(MainThread));
                mainThread.Start();
                //Start server cho van hanh
                op = new TcpOperation();
                op.Start(configs.opmax, configs.opport, configs.opip);
                //Start server cho xu ly modem va hes
                ports = new Dictionary<int, TcpProxy>();
                foreach (ProxyConfig item in configs.ports)
                {
                    var proxy = new TcpProxy();
                    proxy.Start(item.maxconnection, item.forwardIp, item.forwardPort, item.localPort, item.localIp,configs.opip,configs.opport);
                    ports.Add(item.localPort, proxy);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past,
                    // aka it's time to execute another tick
                    ThreadManager.UpdateMain(); // Execute game logic
                    // Calculate at what point in time the next tick should be executed
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future,
                        // aka the server is NOT running behind
                        // Let the thread sleep until it's needed again.
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}

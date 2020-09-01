using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProxyServer.Models;

namespace ProxyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var configJson = System.IO.File.ReadAllText("config.json");

            var configs = JsonSerializer.Deserialize<Dictionary<string, ProxyConfig>>(configJson);
            Task.WhenAll(configs.Select(c =>
            {
                if (c.Value.protocol == "tcp")
                {
                    var proxy = new TcpProxy();
                    return proxy.Start(c.Value.maxconnection,c.Value.forwardIp, c.Value.forwardPort, c.Value.localPort, c.Value.localIp);
                }
                else
                {
                    return null;
                }


            })).Wait();
        }
    }
}

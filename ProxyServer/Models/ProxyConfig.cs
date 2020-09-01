using System;
namespace ProxyServer.Models
{
    public class ProxyConfig
    {
        public int maxconnection { get; set; }
        public string protocol { get; set; }
        public ushort localPort { get; set; }
        public string localIp { get; set; }
        public string forwardIp { get; set; }
        public ushort forwardPort { get; set; }
    }
}

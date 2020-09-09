using System;
using System.Threading.Tasks;

namespace ProxyServer.Models
{
    public interface IProxy
    {
        void Start(int maxconnect,string remoteServerIp, ushort remoteServerPort, ushort localPort, string localIp = null);
    }
}

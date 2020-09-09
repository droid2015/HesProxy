using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyServer.Models
{
    public class Vanhanh
    {
        public string opip { set; get; }
        public int opport { set; get; }
        public int opmax { set; get; }
        public List<ProxyConfig> ports { set; get; }
    }
}

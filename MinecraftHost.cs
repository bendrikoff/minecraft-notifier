using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MInecraft_Notifier
{
    internal class MinecraftHost
    {
        public readonly string Ip;
        public readonly int Port;

        public MinecraftHost(string ip, int port)
        {
            Ip = ip; 
            Port = port;
        }
    }
}

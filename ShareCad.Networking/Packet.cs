using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ShareCad.Networking
{
    public abstract class Packet
    {
        public abstract void Parse();
        public abstract byte[] Serialize();
    }
}

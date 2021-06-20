using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareCad.Networking.Packets
{
    public class DocumentRequest : Packet
    {
        public DocumentRequest()
        {
            PacketType = PacketType.DocumentRequest;
        }

        public override void Parse()
        {
        }

        public override byte[] Serialize()
        {
            return new byte[] { (byte)PacketType };
        }
    }
}

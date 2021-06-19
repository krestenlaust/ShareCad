using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShareCad.Networking.Packets
{
    public class CursorUpdateServer : Packet
    {
        public byte CollaboratorID { get; private set; }
        public Point Position { get; private set; }
        private readonly byte[] serializedData;

        public CursorUpdateServer(byte collaboratorID, Point position)
        {
            throw new NotImplementedException();
        }

        public CursorUpdateServer(Stream stream)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            throw new NotImplementedException();
        }

        public override byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShareCad.Networking.Packets
{
    public class CursorUpdateClient : Packet
    {
        public Point Position { get; private set; }
        private readonly byte[] serializedData;

        public CursorUpdateClient(Point position)
        {
            PacketType = PacketType.CursorUpdate;

            throw new NotImplementedException();
        }

        public CursorUpdateClient(Stream stream)
        {
            PacketType = PacketType.CursorUpdate;

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

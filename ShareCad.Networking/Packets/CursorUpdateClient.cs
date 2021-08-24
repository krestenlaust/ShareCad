using System;
using System.IO;
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

            Position = position;
        }

        public CursorUpdateClient(Stream stream)
        {
            PacketType = PacketType.CursorUpdate;

            serializedData = new byte[sizeof(double) * 2 + sizeof(bool)];
            stream.Read(serializedData, 0, serializedData.Length);
        }

        public override void Parse()
        {
            double x = BitConverter.ToDouble(serializedData, 0);
            double y = BitConverter.ToDouble(serializedData, sizeof(double));

            Position = new Point(x, y);
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[1 + sizeof(double) * 2 + sizeof(bool)];
            data[0] = (byte)PacketType;
            BitConverter.GetBytes(Position.X).CopyTo(data, 1);
            BitConverter.GetBytes(Position.Y).CopyTo(data, 1 + sizeof(double));

            return data;
        }
    }
}

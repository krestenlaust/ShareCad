using System;
using System.IO;
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
            PacketType = PacketType.CursorUpdate;

            CollaboratorID = collaboratorID;
            Position = position;
        }

        public CursorUpdateServer(Stream stream)
        {
            PacketType = PacketType.CursorUpdate;

            CollaboratorID = (byte)stream.ReadByte();

            serializedData = new byte[sizeof(double) * 2];
            stream.Read(serializedData, 0, sizeof(double) * 2);
        }

        public override void Parse()
        {
            double x = BitConverter.ToDouble(serializedData, 0);
            double y = BitConverter.ToDouble(serializedData, sizeof(double));

            Position = new Point(x, y);
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[sizeof(double) * 2 + 2];
            data[0] = (byte)PacketType;
            data[1] = CollaboratorID;
            BitConverter.GetBytes(Position.X).CopyTo(data, 2);
            BitConverter.GetBytes(Position.Y).CopyTo(data, 2 + sizeof(double));

            return data;
        }
    }
}

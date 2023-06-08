using System;
using System.IO;
using System.Windows;

namespace ShareCad.Networking.Packets
{
    public class CursorUpdateServer : Packet
    {
        public byte CollaboratorID { get; private set; }
        public Point Position { get; private set; }
        public bool DestroyCrosshair { get; private set; }

        readonly byte[] serializedData;

        public CursorUpdateServer(byte collaboratorID, Point position, bool destroyCrosshair)
        {
            PacketType = PacketType.CursorUpdate;

            CollaboratorID = collaboratorID;
            Position = position;
            DestroyCrosshair = destroyCrosshair;
        }

        public CursorUpdateServer(Stream stream)
        {
            PacketType = PacketType.CursorUpdate;

            CollaboratorID = (byte)stream.ReadByte();

            serializedData = new byte[sizeof(double) * 2 + sizeof(bool)];
            stream.Read(serializedData, 0, sizeof(double) * 2 + sizeof(bool));
        }

        public override void Parse()
        {
            double x = BitConverter.ToDouble(serializedData, 0);
            double y = BitConverter.ToDouble(serializedData, sizeof(double));

            Position = new Point(x, y);

            DestroyCrosshair = serializedData[sizeof(double) * 2] == 1;
        }

        public override byte[] Serialize()
        {
            byte[] data = new byte[sizeof(double) * 2 + 2 + sizeof(bool)];
            byte cursor = 0;
            data[cursor++] = (byte)PacketType;
            data[cursor++] = CollaboratorID;
            BitConverter.GetBytes(Position.X).CopyTo(data, cursor);
            cursor += sizeof(double);
            BitConverter.GetBytes(Position.Y).CopyTo(data, cursor);
            cursor += sizeof(double);

            data[cursor++] = BitConverter.GetBytes(DestroyCrosshair)[0];

            return data;
        }
    }
}

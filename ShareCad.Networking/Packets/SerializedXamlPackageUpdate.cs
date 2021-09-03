using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShareCad.Networking.Packets
{
    public class SerializedXamlPackageUpdate : AssetPacket
    {
        public readonly byte[] SerializedXaml;

        public SerializedXamlPackageUpdate(Stream sourceStream) : base(sourceStream)
        {
            PacketType = PacketType.SerializedXamlPackageUpdate;

            // deserialize length of data
            byte[] lengthBytes = new byte[sizeof(int)];
            sourceStream.Read(lengthBytes, 0, lengthBytes.Length);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            // store data
            SerializedXaml = new byte[length];
            sourceStream.Read(SerializedXaml, 0, length);
        }

        public SerializedXamlPackageUpdate(int id, byte[] serializedData) : base(id)
        {
            PacketType = PacketType.SerializedXamlPackageUpdate;

            SerializedXaml = serializedData;
        }

        public override void Parse()
        {
        }

        public override byte[] Serialize()
        {
            byte[] assetBasePacket = base.Serialize();

            byte[] packet = new byte[1 + assetBasePacket.Length + sizeof(int) + SerializedXaml.Length];

            int cursor = 0;
            packet[cursor++] = (byte)PacketType;

            assetBasePacket.CopyTo(packet, cursor);
            cursor += assetBasePacket.Length;

            BitConverter.GetBytes(SerializedXaml.Length).CopyTo(packet, cursor);
            cursor += sizeof(int);

            SerializedXaml.CopyTo(packet, cursor);
            cursor += SerializedXaml.Length;

            return packet;
        }
    }
}

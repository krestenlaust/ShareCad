using System;
using System.IO;
using System.Text;
using System.Xml;

namespace ShareCad.Networking.Packets
{
    public class DocumentUpdate : Packet
    {
        public XmlDocument XmlDocument { get; private set; }
        public readonly byte[] serializedData;

        public DocumentUpdate(Stream stream)
        {
            PacketType = PacketType.DocumentUpdate;

            byte[] lengthBytes = new byte[sizeof(int)];
            stream.Read(lengthBytes, 0, lengthBytes.Length);

            int length = BitConverter.ToInt32(lengthBytes, 0);

            serializedData = new byte[length];
            stream.Read(serializedData, 0, length);
        }

        public DocumentUpdate(XmlDocument xmlDocument)
        {
            PacketType = PacketType.DocumentUpdate;

            this.XmlDocument = xmlDocument;
        }

        public override void Parse()
        {
            XmlDocument = new XmlDocument();
            XmlDocument.LoadXml(Encoding.UTF8.GetString(serializedData));
        }

        /// <summary>
        /// [byte] PacketType, [int] Length, [UTF-8] Document content.
        /// </summary>
        /// <returns></returns>
        public override byte[] Serialize()
        {
            byte[] data = Encoding.UTF8.GetBytes(XmlDocument.OuterXml);

            byte[] packet = new byte[sizeof(byte) + sizeof(int) + data.Length];
            packet[0] = (byte)PacketType;
            data.CopyTo(packet, sizeof(int) + sizeof(byte));

            BitConverter.GetBytes(data.Length).CopyTo(packet, sizeof(byte));

            return packet;
        }
    }
}

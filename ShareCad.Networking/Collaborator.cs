using System.Net.Sockets;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking
{
    internal class Collaborator
    {
        public TcpClient TcpClient;
        public XmlDocument Document;
        public Point CursorLocation;
        public byte ID;

        public Collaborator(byte id, TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
            ID = id;
        }
    }
}

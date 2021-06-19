using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking
{
    internal class Collaborator
    {
        public TcpClient TcpClient;
        public XmlDocument Document;
        public Point CursorLocation;

        public Collaborator(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
        }
    }
}

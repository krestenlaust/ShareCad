using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ShareCad.Networking
{
    public static class Networking
    {
        const short PORT = 4040;
        private static TcpClient tcpClient;

        public enum ClientType
        {
            Client,
            Server
        }

        public static event Action onConnectFinished;

        public static void Debug() => Connect(IPAddress.Loopback);

        public static void SendObject<T>(object emne)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            serializer.Serialize(tcpClient.GetStream(), emne);
        }

        public static void BindListener()
        {

        }

        public static void Connect(IPAddress address)
        {
            tcpClient = new TcpClient();
            tcpClient.BeginConnect(address, PORT, new AsyncCallback(ConnectionComplete), null);
        }

        public static void ConnectionComplete(IAsyncResult ar)
        {
            tcpClient.EndConnect(ar);
            onConnectFinished?.Invoke();
        }
    }
}

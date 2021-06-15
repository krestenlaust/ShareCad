using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows;
using System.IO;

namespace Networking
{
    public static class Networking
    {
        public const short Port = 4040;

        /// <summary>
        /// True if bound to a port, or connected to a host.
        /// </summary>
        private static bool isActive => !(Client.HostClient is null && Server.Clients is null);

        public static void TransmitStream(Stream source)
        {
            if (!isActive)
            {
                return;
            }

            if (!(Server.Clients is null))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    source.CopyTo(memoryStream);
                    foreach (var client in Server.Clients)
                    {
                        NetworkStream clientStream = client.GetStream();
                        memoryStream.CopyTo(clientStream);
                        memoryStream.Position = 0;
                    }
                }
            }
            else if (!(Client.HostClient is null))
            {
                NetworkStream hostStream = Client.HostClient.GetStream();
                source.CopyTo(hostStream);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Whether data was available.</returns>
        public static bool ReceiveStream(out NetworkStream stream)
        {
            if (!isActive)
            {
                stream = null;
                return false;
            }

            if (!(Server.Clients is null))
            {
                // is host.
                foreach (var client in Server.Clients)
                {
                    stream = client.GetStream();

                    if (stream.DataAvailable)
                    {
                        return true;
                    }
                }
            }
            else if (!(Client.HostClient is null))
            {
                // is client.
                stream = Client.HostClient.GetStream();

                return stream.DataAvailable;
            }

            stream = null;
            return false;
        }

        public static class Client
        {
            public static TcpClient HostClient { get; private set; } = null;

            public enum ConnectStatus
            {
                Failed,
                Established
            }

            public static event Action<ConnectStatus> OnConnectFinished;

            public static void DebugConnectLoopback() => Connect(IPAddress.Loopback);

            public static void Disconnect()
            {
                HostClient?.Close();
                HostClient = null;

                Console.WriteLine("Disconnected");
            }

            /// <summary>
            /// Connects to a host.
            /// </summary>
            /// <param name="address"></param>
            public static void Connect(IPAddress address)
            {
                if (!(HostClient is null))
                {
                    Console.WriteLine("Not disconnected.");
                    return;
                }

                HostClient = new TcpClient();

                HostClient.BeginConnect(address, Port, new AsyncCallback(delegate (IAsyncResult ar) {
                    try
                    {
                        HostClient.EndConnect(ar);
                        OnConnectFinished?.Invoke(ConnectStatus.Established);
                        Console.WriteLine("Connected to host: " + HostClient.Client.RemoteEndPoint);
                    }
                    catch (SocketException)
                    {
                        OnConnectFinished?.Invoke(ConnectStatus.Failed);
                    }
                }), null);
            }
        }

        public static class Server
        {
            public static List<TcpClient> Clients { get; private set; } = null;
            private static TcpListener listener;

            public static void BindListener(IPAddress bindAddress)
            {
                if (!(Clients is null))
                {
                    return;
                }

                Clients = new List<TcpClient>();

                listener = new TcpListener(bindAddress, Port);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(delegate (IAsyncResult ar)
                {
                    TcpClient newClient = listener.EndAcceptTcpClient(ar);
                    Clients.Add(newClient);

                    Console.WriteLine("A client connected: " + newClient.Client.RemoteEndPoint);
                }), null);

                Console.WriteLine("Bound listener: " + listener.LocalEndpoint);
            }
        }
    }
}

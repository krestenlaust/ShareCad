using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShareCad
{
    public static class Networking
    {
        public const short Port = 4040;

        /// <summary>
        /// True if bound to a port, or connected to a host.
        /// </summary>
        private static bool isActive => !(Client.HostClient is null && Server.Clients is null);
        private static bool isHost => !(Server.Clients is null);
        private static bool isClient => !(Client.HostClient is null);

        public static void Transmit(string stringData)
        {
            if (!isActive)
            {
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(stringData);
            byte[] dataLengthBytes = BitConverter.GetBytes(data.Length);

            if (isHost)
            {
                foreach (var client in Server.Clients)
                {
                    NetworkStream clientStream = client.GetStream();

                    clientStream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
                    clientStream.Write(data, 0, data.Length);
                }
            }
            else if (isClient)
            {
                NetworkStream hostStream = Client.HostClient.GetStream();

                hostStream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
                hostStream.Write(data, 0, data.Length);
            }
        }

        public static void Transmit(Stream source)
        {
            if (!isActive)
            {
                return;
            }

            int sourceDataLength = (int)source.Length;

            byte[] data = new byte[source.Length];
            byte[] dataLengthBytes = BitConverter.GetBytes(sourceDataLength);

            // copy data into byte array.
            source.Read(data, 0, sourceDataLength);

            if (isHost)
            {
                foreach (var client in Server.Clients)
                {
                    NetworkStream clientStream = client.GetStream();

                    clientStream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
                    clientStream.Write(data, 0, data.Length);
                }
            }
            else if (isClient)
            {
                NetworkStream hostStream = Client.HostClient.GetStream();

                hostStream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
                hostStream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Whether data was available.</returns>
        public static bool ReceiveXml(out string readXml)
        {
            if (!isActive)
            {
                readXml = null;
                return false;
            }

            if (!(Server.Clients is null))
            {
                // is host.
                foreach (var client in Server.Clients)
                {
                    NetworkStream stream = client.GetStream();

                    if (stream.DataAvailable)
                    {
                        byte[] xmlData = ReadBytesFromStream(stream);
                        readXml = Encoding.ASCII.GetString(xmlData);
                        return true;
                    }

                    // only read first client found with data. For.. reasons...
                    break;
                }
            }
            else if (!(Client.HostClient is null))
            {
                // is client.
                NetworkStream stream = Client.HostClient.GetStream();

                if (stream.DataAvailable)
                {
                    byte[] xmlData = ReadBytesFromStream(stream);
                    readXml = Encoding.ASCII.GetString(xmlData);
                    return true;
                }
            }

            readXml = null;
            return false;
        }

        /// <summary>
        /// Reads the amount of bytes specified by the first integer (4 bytes) of the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static byte[] ReadBytesFromStream(NetworkStream stream)
        {
            byte[] dataLengthBytes = new byte[sizeof(int)];
            stream.Read(dataLengthBytes, 0, sizeof(int));

            int dataLength = BitConverter.ToInt32(dataLengthBytes, 0);

            byte[] data = new byte[dataLength];
            stream.Read(data, 0, dataLength);

            return data;
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

                HostClient.BeginConnect(address, Port, new AsyncCallback(delegate (IAsyncResult ar)
                {
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

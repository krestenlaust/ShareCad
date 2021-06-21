using ShareCad.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking
{
    public enum PacketType : byte
    {
        DocumentUpdate = 1,
        DocumentRequest = 2,
        CursorUpdate = 3,
    }

    /// <summary>
    /// The responsibilities of a <c>NetworkManager</c>-instance.
    /// </summary>
    public enum NetworkFunction
    {
        Host,
        Guest
    }

    /// <summary>
    /// Manages networking in a different thread.
    /// </summary>
    public class NetworkManager
    {
        public const short DefaultPort = 4040;
        public const int NetworkUpdateInterval = 500;

        private readonly NetworkFunction networkRole;
        private Thread networkThread;
        private bool networkRunning = true;
        private Server server;

        public Client Client { get; private set; }

        public NetworkManager(NetworkFunction networkRole)
        {
            Client = new Client();
            this.networkRole = networkRole;
        }

        /// <summary>
        /// Initializes server at endpoint if Host, and a client connecting to the endpoint.
        /// </summary>
        /// <param name="serverEndpoint"></param>
        /// <exception cref="SocketException"></exception>
        public void Start(IPAddress address, int port = DefaultPort) => Start(new IPEndPoint(address, port));

        /// <summary>
        /// Initializes server at endpoint if Host, and a client connecting to the endpoint.
        /// </summary>
        /// <param name="serverEndpoint"></param>
        /// <exception cref="SocketException"></exception>
        public void Start(IPEndPoint endPoint)
        {
            if (networkRole == NetworkFunction.Host)
            {
                server = new Server(endPoint);
            }

            Client.Connect(new IPEndPoint(IPAddress.Loopback, endPoint.Port));

            networkThread = new Thread(NetworkLoop);
            networkThread.Start();
        }

        /// <summary>
        /// Stops any client/server running.
        /// </summary>
        public void Stop()
        {
            networkRunning = false;
        }

        private void NetworkLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (networkRunning)
            {
                stopwatch.Restart();

                // Handle server logic.
                if (server is Server)
                {
                    server.Update();
                }

                // Handle client logic.
                if (Client.HostClient.Connected)
                {
                    Client.Update();
                }

                while (stopwatch.ElapsedMilliseconds < NetworkUpdateInterval)
                    Thread.Sleep(0);
            }

            server.Dispose();
            Client.Dispose();
        }
        
        public void SendDocument(XmlDocument document) => SendPacket(new DocumentUpdate(document));

        public void SendCursorPosition(Point position) => SendPacket(new CursorUpdateClient(position));

        private void SendPacket(Packet packet)
        {
            if (Client?.HostClient is null)
            {
                return;
            }

            NetworkStream stream = Client.HostClient.GetStream();

            byte[] data = packet.Serialize();
            stream.Write(data, 0, data.Length);
        }

        /*
        public static void Transmit(NetworkStream stream, string stringData)
        {
            byte[] data = Encoding.UTF8.GetBytes(stringData);
            byte[] dataLengthBytes = BitConverter.GetBytes(data.Length);

            stream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
            stream.Write(data, 0, data.Length);
        }*/

        /*
        /// <summary>
        /// Reads the next X bytes and converts to UTF8-string, where X is the first integer in the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Whether data was available.</returns>
        public static string ReceiveTextFromStream(NetworkStream stream)
        {
            byte[] dataLengthBytes = new byte[sizeof(int)];
            stream.Read(dataLengthBytes, 0, sizeof(int));

            int dataLength = BitConverter.ToInt32(dataLengthBytes, 0);

            byte[] data = new byte[dataLength];
            stream.Read(data, 0, dataLength);

            return Encoding.UTF8.GetString(data);
        }*/
    }
}

using ShareCad.Networking.Packets;
using System;
using System.Collections.Generic;
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

    public enum NetworkFunction
    {
        Host,
        Guest
    }

    public class NetworkManager
    {
        public const short DefaultPort = 4040;
        public const int NetworkUpdateInterval = 1000;

        private Thread networkThread;
        private bool networkRunning = true;
        private NetworkFunction networkRole;

        public Client Client { get; private set; }
        public Server Server { get; private set; }

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
                Server = new Server(endPoint);
            }

            Client.Connect(endPoint);

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
            while (networkRunning)
            {
                // Handle server logic.
                if (Server is Server)
                {
                    Server.Update();
                }

                // Handle client logic.
                if (Client.HostClient.Connected)
                {
                    Client.Update();
                }

                Thread.Sleep(NetworkUpdateInterval);
            }

            Server.Dispose();
            Client.Dispose();
        }
        
        public void SendDocument(XmlDocument document) => SendPacket(PacketType.DocumentUpdate, new DocumentUpdate(document));

        public void SendCursorPosition(Point position) => SendPacket(PacketType.CursorUpdate, new CursorUpdateClient(position));

        private void SendPacket(PacketType packetType, Packet packet)
        {
            if (Client?.HostClient is null)
            {
                return;
            }

            NetworkStream stream = Client.HostClient.GetStream();

            stream.WriteByte((byte)packetType);
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
        }
    }
}

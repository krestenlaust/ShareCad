using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Xml;
using ShareCad.Logging;
using ShareCad.Networking.Packets;

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
        public const int NetworkUpdateInterval = 100;

        private readonly NetworkFunction networkRole;
        private Thread networkThread;
        private bool networkRunning = true;
        private Server server;
        private readonly Queue<Packet> packetsToSend = new Queue<Packet>();
        private readonly Logger logger = new Logger("Client/Manager", false);
        private TcpClient hostClient = null;

        public NetworkManager(NetworkFunction networkRole)
        {
            hostClient = new TcpClient();
            this.networkRole = networkRole;
        }

        public enum ConnectStatus
        {
            Failed,
            Established
        }

        public event Action<ConnectStatus> OnConnectFinished;
        /// <summary>
        /// Called when to update worksheet.
        /// </summary>
        public event Action<XmlDocument> OnWorksheetUpdate;
        /// <summary>
        /// Called when another collaborator moves their cursor.
        /// </summary>
        public event Action<byte, Point> OnCollaboratorCursorUpdate;


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

                // connect locally.
                ConnectClient(new IPEndPoint(IPAddress.Loopback, endPoint.Port));
            }
            else
            {
                // connect remotely.
                ConnectClient(endPoint);
            }

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

                // Send queued packets.
                while (packetsToSend.Count > 0)
                {
                    TransmitPacket(packetsToSend.Dequeue());
                }

                // Handle server logic.
                if (server is Server)
                {
                    server.Update();
                }

                // Handle client logic.
                if (hostClient.Connected)
                {
                    UpdateClient();
                }

                while (stopwatch.ElapsedMilliseconds < NetworkUpdateInterval)
                    Thread.Sleep(0);
            }

            server.Dispose();
        }
        
        public void SendDocument(XmlDocument document) => EnqueuePacket(new DocumentUpdate(document));

        public void SendCursorPosition(Point position) => EnqueuePacket(new CursorUpdateClient(position));

        private void EnqueuePacket(Packet packet)
        {
            packetsToSend.Enqueue(packet);
        }

        private void TransmitPacket(Packet packet)
        {
            if (hostClient is null || !hostClient.Connected)
            {
                return;
            }

            NetworkStream stream = hostClient.GetStream();

            byte[] data = packet.Serialize();
            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Connects to a host by specified address and port.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <exception cref="SocketException">Connection failed.</exception>
        private void ConnectClient(IPEndPoint endPoint)
        {
            /// TODO: implement error-handling.
            hostClient.BeginConnect(endPoint.Address, endPoint.Port, new AsyncCallback(delegate (IAsyncResult ar)
            {
                try
                {
                    hostClient.EndConnect(ar);
                    OnConnectFinished?.Invoke(ConnectStatus.Established);
                    logger.Print($"Connected to host on {hostClient.Client.RemoteEndPoint}");
                }
                catch (SocketException ex)
                {
                    logger.PrintError(ex);
                    OnConnectFinished?.Invoke(ConnectStatus.Failed);
                }
            }), null);
        }

        public void DisconnectClient()
        {
            hostClient?.Close();
            hostClient = null;

            logger.Print("Disconnected");
        }

        private void UpdateClient()
        {
            NetworkStream stream = hostClient.GetStream();

            if (!stream.DataAvailable)
            {
                return;
            }

            // data is available.
            PacketType packetType = (PacketType)stream.ReadByte();

            switch (packetType)
            {
                case PacketType.DocumentUpdate:
                    DocumentUpdate documentUpdate = new DocumentUpdate(stream);
                    documentUpdate.Parse();

                    OnWorksheetUpdate?.Invoke(documentUpdate.XmlDocument);
                    break;
                case PacketType.DocumentRequest: // not valid on client.
                    break;
                case PacketType.CursorUpdate:
                    CursorUpdateServer cursorUpdate = new CursorUpdateServer(stream);
                    cursorUpdate.Parse();

                    OnCollaboratorCursorUpdate?.Invoke(cursorUpdate.CollaboratorID, cursorUpdate.Position);
                    break;
                default:
                    break;
            }
        }
    }
}

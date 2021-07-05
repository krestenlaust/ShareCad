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
    /// The responsibilities of a <c>NetworkClient</c>-instance.
    /// </summary>
    public enum NetworkFunction
    {
        Host,
        Guest
    }

    /// <summary>
    /// Manages networking in a different thread.
    /// </summary>
    public class NetworkClient
    {
        private readonly Queue<Packet> packetsToSend = new Queue<Packet>();
        private readonly Logger log;
        public readonly TcpClient HostClient;
        public readonly IPEndPoint Endpoint;

        public NetworkClient(IPEndPoint endpoint)
        {
            log = new Logger("Client/Manager", false);
            HostClient = new TcpClient();
            this.Endpoint = endpoint;
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


        /*
        ///<summary>
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

        }*/

        /// <summary>
        /// Connects to the host configured in constructor.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <exception cref="SocketException">Connection failed.</exception>
        public void Connect()
        {
            /// TODO: implement error-handling.
            HostClient.BeginConnect(Endpoint.Address, Endpoint.Port, new AsyncCallback(delegate (IAsyncResult ar)
            {
                try
                {
                    HostClient.EndConnect(ar);
                    OnConnectFinished?.Invoke(ConnectStatus.Established);
                    log.Print($"Connected to host on {HostClient.Client.RemoteEndPoint}");
                }
                catch (SocketException ex)
                {
                    log.PrintError(ex);
                    OnConnectFinished?.Invoke(ConnectStatus.Failed);
                }
            }), null);
        }

        public void DisconnectClient()
        {
            HostClient?.Close();

            log.Print("Disconnected");
        }

        public void SendAllQueuedPackets()
        {
            while (packetsToSend.Count > 0)
            {
                TransmitPacket(packetsToSend.Dequeue());
            }
        }

        public void Update()
        {
            NetworkStream stream = HostClient.GetStream();

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

        public void SendDocument(XmlDocument document) => EnqueuePacket(new DocumentUpdate(document));

        public void SendCursorPosition(Point position) => EnqueuePacket(new CursorUpdateClient(position));

        private void EnqueuePacket(Packet packet)
        {
            packetsToSend.Enqueue(packet);
        }

        private void TransmitPacket(Packet packet)
        {
            if (HostClient is null || !HostClient.Connected)
            {
                return;
            }

            NetworkStream stream = HostClient.GetStream();

            byte[] data = packet.Serialize();
            stream.Write(data, 0, data.Length);
        }
    }
}

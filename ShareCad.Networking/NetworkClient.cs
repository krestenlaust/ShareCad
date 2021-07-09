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
        public bool isConnecting { get; private set; }

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

        /// <summary>
        /// Connects to the host configured in constructor.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <exception cref="SocketException">Connection failed.</exception>
        public void Connect()
        {
            isConnecting = true;

            // TODO: implement error-handling.
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

                isConnecting = false;
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

            Dictionary<PacketType, Packet> packets = new Dictionary<PacketType, Packet>();
            Dictionary<byte, Point> cursorPositions = new Dictionary<byte, Point>();

            while (stream.DataAvailable)
            {
                // data is available.
                PacketType packetType = (PacketType)stream.ReadByte();
                Packet packet = null;

                switch (packetType)
                {
                    case PacketType.DocumentUpdate:
                        packet = new DocumentUpdate(stream);
                        break;
                    case PacketType.DocumentRequest: // not valid on client.
                        break;
                    case PacketType.CursorUpdate:
                        CursorUpdateServer packet2 = new CursorUpdateServer(stream);

                        packet2.Parse();
                        cursorPositions[packet2.CollaboratorID] = packet2.Position;
                        continue;
                    default:
                        break;
                }

                packets[packetType] = packet;
            }

            foreach (var item in packets.Values)
            {
                item.Parse();

                switch (item)
                {
                    case DocumentUpdate documentUpdate:
                        OnWorksheetUpdate?.Invoke(documentUpdate.XmlDocument);
                        break;
                    case CursorUpdateServer _: // handled independently.
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in cursorPositions)
            {
                OnCollaboratorCursorUpdate?.Invoke(item.Key, item.Value);
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

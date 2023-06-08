using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        SerializedXamlPackageUpdate = 4
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
        public readonly TcpClient HostClient;
        public readonly IPEndPoint Endpoint;
        public bool isConnecting { get; private set; }
        
        readonly Queue<Packet> packetsToSend = new Queue<Packet>();
        readonly Logger log;
        bool isDisconnected = false;

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
        public event Action<int, byte[]> OnXamlPackageUpdate;
        /// <summary>
        /// Called when another collaborator moves their cursor.
        /// </summary>
        public event Action<byte, Point, bool> OnCollaboratorCursorUpdate;

        public event Action OnDisconnected;

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
                isConnecting = false;

                try
                {
                    HostClient.EndConnect(ar);
                    OnConnectFinished?.Invoke(ConnectStatus.Established);
                    isDisconnected = false;
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
            OnDisconnected?.Invoke();
            isDisconnected = true;
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

            Dictionary<PacketType, Packet> uniquePackets = new Dictionary<PacketType, Packet>();
            Dictionary<byte, (Point, bool)> cursorUpdates = new Dictionary<byte, (Point, bool)>();
            List<Packet> otherPackets = new List<Packet>();

            while (stream.DataAvailable)
            {
                // data is available.
                PacketType packetType = (PacketType)stream.ReadByte();
                Packet uniquePacket = null;

                switch (packetType)
                {
                    case PacketType.DocumentUpdate:
                        uniquePacket = new DocumentUpdate(stream);
                        break;
                    case PacketType.DocumentRequest: // not valid on client.
                        break;
                    case PacketType.CursorUpdate:
                        CursorUpdateServer packet2 = new CursorUpdateServer(stream);

                        packet2.Parse();
                        cursorUpdates[packet2.CollaboratorID] = (packet2.Position, packet2.DestroyCrosshair);
                        continue;
                    case PacketType.SerializedXamlPackageUpdate:
                        otherPackets.Add(new SerializedXamlPackageUpdate(stream));
                        break;
                    default:
                        break;
                }

                if (uniquePacket is null)
                {
                    continue;
                }

                uniquePackets[packetType] = uniquePacket;
            }

            otherPackets.AddRange(uniquePackets.Values);

            foreach (Packet item in otherPackets)
            {
                try
                {
                    item.Parse();
                }
                catch (Exception ex)
                {
                    log.PrintError("Tried parsing invalid packet: " + ex);
                    continue;
                }

                switch (item)
                {
                    case DocumentUpdate documentUpdate:
                        OnWorksheetUpdate?.Invoke(documentUpdate.XmlDocument);
                        break;
                    case CursorUpdateServer _: // handled independently.
                        break;
                    case SerializedXamlPackageUpdate xamlPackage:
                        OnXamlPackageUpdate?.Invoke(xamlPackage.ID, xamlPackage.SerializedXaml);
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in cursorUpdates)
            {
                OnCollaboratorCursorUpdate?.Invoke(item.Key, item.Value.Item1, item.Value.Item2);
            }
        }

        public void SendDocument(XmlDocument document) => EnqueuePacket(new DocumentUpdate(document));

        public void SendCursorPosition(Point position) => EnqueuePacket(new CursorUpdateClient(position));

        public void SendAsset(AssetPacket packet) => EnqueuePacket(packet);

        void EnqueuePacket(Packet packet)
        {
            packetsToSend.Enqueue(packet);
        }

        void TransmitPacket(Packet packet)
        {
            try
            {
                NetworkStream stream = HostClient.GetStream();

                byte[] data = packet.Serialize();
                stream.Write(data, 0, data.Length);
            }
            catch (IOException)
            {
                if (!isDisconnected)
                {
                    OnDisconnected?.Invoke();
                    isDisconnected = true;
                }
            }
        }
    }
}

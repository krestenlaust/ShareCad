using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Xml;
using ShareCad.Logging;
using ShareCad.Networking.Packets;

namespace ShareCad.Networking
{
    internal class Server : IDisposable
    {
        //private const string EmptyDocumentXML = "<worksheet xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ve=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:ws=\"http://schemas.mathsoft.com/worksheet50\" xmlns:ml=\"http://schemas.mathsoft.com/math50\" xmlns:u=\"http://schemas.mathsoft.com/units10\" xmlns:p=\"http://schemas.mathsoft.com/provenance10\" xmlns=\"http://schemas.mathsoft.com/worksheet50\"><regions /></worksheet>";

        private readonly TcpListener listener;
        private readonly List<Collaborator> clients;
        private readonly HashSet<Collaborator> disconnectedClients = new HashSet<Collaborator>();
        private readonly Logger logger = new Logger("Server", false);
        private readonly HashSet<AssetPacket> assets = new HashSet<AssetPacket>();
        private XmlDocument currentDocument;
        private byte availableCollaboratorID;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindAddress"></param>
        /// <exception cref="SocketException">Thrown when listener can't be established.</exception>
        public Server(IPEndPoint bindAddress)
        {
            //currentDocument = new XmlDocument();
            //currentDocument.LoadXml(EmptyDocumentXML);

            clients = new List<Collaborator>();

            /// TODO: Improve error-handling.
            try
            {
                listener = new TcpListener(bindAddress);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnected), null);

                logger.Print($"Bound listener on {listener.LocalEndpoint}");
            }
            catch (SocketException ex)
            {
                logger.PrintError(ex);
                throw;
            }
        }

        /// <summary>
        /// Updates internal state and performs server tasks.
        /// </summary>
        internal void Update()
        {
            bool documentSent = false;

            foreach (Collaborator currentCollaborator in clients)
            {
                if (currentCollaborator?.TcpClient.Connected != true)
                {
                    disconnectedClients.Add(currentCollaborator);
                    continue;
                }

                NetworkStream stream = currentCollaborator.TcpClient.GetStream();

                // stores the type of packets where only the most recent one is relevant.
                Dictionary<PacketType, Packet> uniquePackets = new Dictionary<PacketType, Packet>();
                // stores packets where multiple of the same packet are reasonable.
                List<Packet> nonUniquePackets = new List<Packet>();

                while (stream.DataAvailable)
                {
                    Packet packet = null;
                    bool unique = false;

                    PacketType packetType = (PacketType)stream.ReadByte();
                    switch (packetType)
                    {
                        case PacketType.DocumentUpdate:
                            packet = new DocumentUpdate(stream);
                            unique = true;

                            // Already recieved a new document this update.
                            if (documentSent)
                            {
                                continue;
                            }
                            break;
                        case PacketType.DocumentRequest:
                            // send latest version of document to collaborator.
                            packet = new DocumentRequest();
                            unique = true;
                            break;
                        case PacketType.CursorUpdate:
                            // update local cursor position of collaborator.
                            packet = new CursorUpdateClient(stream);
                            unique = true;
                            break;
                        case PacketType.SerializedXamlPackageUpdate:
                            packet = new SerializedXamlPackageUpdate(stream);
                            break;
                        default:
                            break;
                    }

                    if (packet is null)
                    {
                        continue;
                    }

                    if (unique)
                    {
                        uniquePackets[packetType] = packet;
                    }
                    else
                    {
                        nonUniquePackets.Add(packet);
                    }
                }

                // Combine packets, non-uniques before uniques.
                nonUniquePackets.AddRange(uniquePackets.Values);

                /// TODO: Sort packets to make documentrequest the last one.
                foreach (Packet packet in nonUniquePackets)
                {
                    try
                    {
                        packet.Parse();
                    }
                    catch (Exception ex)
                    {
                        logger.PrintError("Tried parsing invalid packet: " + ex);
                        throw ex;
                        continue;
                    }

                    switch (packet)
                    {
                        case SerializedXamlPackageUpdate xamlPackageUpdate:
                            assets.Add(xamlPackageUpdate);
                            SendPacketAll(xamlPackageUpdate.Serialize(), currentCollaborator.ID);
                            break;
                        case DocumentUpdate documentUpdate:
                            currentCollaborator.Document = documentUpdate.XmlDocument;
                            
                            UpdateDocumentAll(documentUpdate.XmlDocument, currentCollaborator.ID);
                            documentSent = true;
                            currentDocument = documentUpdate.XmlDocument;
                            break;
                        case DocumentRequest _:
                            if (currentDocument is null)
                            {
                                break;
                            }

                            byte[] serializedDocumentPacket = new DocumentUpdate(currentDocument).Serialize();

                            try
                            {
                                stream.Write(serializedDocumentPacket, 0, serializedDocumentPacket.Length);
                            }
                            catch (IOException)
                            {
                                logger.PrintError("Collaborator connection closed");
                            }
                            break;
                        case CursorUpdateClient cursorUpdate:
                            currentCollaborator.CursorLocation = cursorUpdate.Position;

                            UpdateCursorAll(currentCollaborator.ID, cursorUpdate.Position, false);
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (Collaborator collaborator in disconnectedClients)
            {
                UpdateCursorAll(collaborator.ID, new Point(), true);

                clients.Remove(collaborator);
                logger.Print("Disconnected client");
            }

            disconnectedClients.Clear();
        }

        private void SendPacketAll(byte[] packet, byte ignoreID=byte.MaxValue)
        {
            foreach (var item in clients)
            {
                if (item?.TcpClient is null)
                {
                    continue;
                }

                if (item.ID == ignoreID)
                {
                    continue;
                }

                NetworkStream stream = item.TcpClient.GetStream();
                try
                {
                    stream.Write(packet, 0, packet.Length);
                }
                catch (IOException)
                {
                    logger.PrintError("Collaborator connection closed");
                }
            }
        }

        private void UpdateDocumentAll(XmlDocument newDocument, byte ignoreID=byte.MaxValue)
        {
            DocumentUpdate packet = new DocumentUpdate(newDocument);
            byte[] serializedPacket = packet.Serialize();

            SendPacketAll(serializedPacket, ignoreID);
        }

        private void UpdateCursorAll(byte collaboratorID, Point position, bool destroyCursor)
        {
            CursorUpdateServer packet = new CursorUpdateServer(collaboratorID, position, destroyCursor);
            byte[] serializedPacket = packet.Serialize();

            SendPacketAll(serializedPacket, collaboratorID);
        }

        private void ClientConnected(IAsyncResult ar)
        {
            TcpClient newClient;

            try
            {
                newClient = listener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnected), null);

            clients.Add(new Collaborator(availableCollaboratorID++, newClient));

            logger.Print($"A collaborator connected on {newClient.Client.RemoteEndPoint}");

            // send latest document state if any.
            if (currentDocument is null)
            {
                return;
            }

            DocumentUpdate packet = new DocumentUpdate(currentDocument);
            byte[] serializedPacket = packet.Serialize();

            NetworkStream stream = newClient.GetStream();
            stream.Write(serializedPacket, 0, serializedPacket.Length);
        }

        public void Dispose()
        {
            foreach (var item in clients)
            {
                item.TcpClient?.Close();
            }

            listener?.Stop();
        }
    }
}

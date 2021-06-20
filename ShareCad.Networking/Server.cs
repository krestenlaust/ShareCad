using ShareCad.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking
{
    public class Server : IDisposable
    {
        private readonly TcpListener listener;
        private readonly List<Collaborator> clients;
        private XmlDocument currentDocument;
        private byte availableCollaboratorID;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindAddress"></param>
        /// <exception cref="SocketException">Thrown when listener can't be established.</exception>
        public Server(IPEndPoint bindAddress)
        {
            clients = new List<Collaborator>();

            /// TODO: Improve error-handling.
            try
            {
                listener = new TcpListener(bindAddress);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnected), null);

                Console.WriteLine("Bound listener: " + listener.LocalEndpoint);
            }
            catch (SocketException)
            {
                Console.Error.WriteLine("Can't establish server");
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
                if (currentCollaborator?.TcpClient is null)
                {
                    continue;
                }

                NetworkStream stream = currentCollaborator.TcpClient.GetStream();

                Dictionary<PacketType, Packet> packets = new Dictionary<PacketType, Packet>();
                
                while (stream.DataAvailable)
                {
                    Packet packet = null;
                    PacketType packetType = (PacketType)stream.ReadByte();
                    switch (packetType)
                    {
                        case PacketType.DocumentUpdate:
                            packet = new DocumentUpdate(stream);

                            // Already recieved a new document this update.
                            if (documentSent)
                            {
                                continue;
                            }
                            break;
                        case PacketType.DocumentRequest:
                            // send latest version of document to collaborator.
                            packet = new DocumentUpdate(currentDocument);
                            break;
                        case PacketType.CursorUpdate:
                            // update local cursor position of collaborator.
                            packet = new CursorUpdateClient(stream);
                            break;
                        default:
                            break;
                    }

                    packets[packetType] = packet;
                }

                /// TODO: Sort packets to make documentrequest the last one.
                foreach (Packet packet in packets.Values)
                {
                    packet.Parse();

                    switch (packet)
                    {
                        case DocumentUpdate documentUpdate:
                            UpdateDocumentAll(documentUpdate.XmlDocument, currentCollaborator.ID);
                            documentSent = true;
                            currentDocument = documentUpdate.XmlDocument;
                            break;
                        case DocumentRequest _:
                            if (currentDocument is null)
                            {
                                break;
                            }

                            byte[] serializedDocumentPacket = packet.Serialize();

                            stream.Write(serializedDocumentPacket, 0, serializedDocumentPacket.Length);
                            break;
                        case CursorUpdateClient cursorUpdate:
                            currentCollaborator.CursorLocation = cursorUpdate.Position;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void UpdateCursorAll(byte collaboratorID, Point position, byte ignoreID=byte.MaxValue)
        {
            CursorUpdateServer packet = new CursorUpdateServer(collaboratorID, position);

            byte[] serializedPacket = packet.Serialize();

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
                stream.Write(serializedPacket, 0, serializedPacket.Length);
            }
        }

        private void UpdateDocumentAll(XmlDocument newDocument, byte ignoreID=byte.MaxValue)
        {
            DocumentUpdate packet = new DocumentUpdate(newDocument);
            byte[] serializedPacket = packet.Serialize();

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
                stream.Write(serializedPacket, 0, serializedPacket.Length);
            }
        }

        private void ClientConnected(IAsyncResult ar)
        {
            TcpClient newClient = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnected), null);

            clients.Add(new Collaborator(availableCollaboratorID++, newClient));

            Console.WriteLine("A collaborator connected on " + newClient.Client.RemoteEndPoint);
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

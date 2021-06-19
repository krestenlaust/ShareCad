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

            foreach (Collaborator item in clients)
            {
                if (item?.TcpClient is null)
                {
                    continue;
                }

                NetworkStream stream = item.TcpClient.GetStream();

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
                            packet = new DocumentRequest();
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

                foreach (Packet packet in packets.Values)
                {
                    packet.Parse();

                    switch (packet)
                    {
                        case DocumentUpdate documentUpdate:
                            UpdateDocumentAll(documentUpdate.XmlDocument);
                            documentSent = true;
                            break;
                        case DocumentRequest documentRequest:
                            break;
                        case CursorUpdateClient cursorUpdate:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void UpdateDocumentAll(XmlDocument newDocument)
        {
            byte[] payload = Encoding.UTF8.GetBytes(newDocument.OuterXml);
            byte[] byteLength = BitConverter.GetBytes(payload.Length);

            foreach (var item in clients)
            {
                if (item?.TcpClient is null)
                {
                    continue;
                }

                NetworkStream stream = item.TcpClient.GetStream();
                stream.WriteByte((byte)PacketType.DocumentUpdate);
                stream.Write(byteLength, 0, byteLength.Length);
                stream.Write(payload, 0, payload.Length);
            }

            currentDocument = newDocument;
        }

        private void ClientConnected(IAsyncResult ar)
        {
            TcpClient newClient = listener.EndAcceptTcpClient(ar);
            clients.Add(new Collaborator(newClient));

            Console.WriteLine("A client connected: " + newClient.Client.RemoteEndPoint);
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

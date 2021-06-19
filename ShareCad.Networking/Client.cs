using ShareCad.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking
{
    public class Client : IDisposable
    {
        public TcpClient HostClient { get; private set; } = null;

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
        /// Call <c>Connect</c> to connect client.
        /// </summary>
        public Client()
        {
            HostClient = new TcpClient();
        }

        /// <summary>
        /// Connects to a host by specified address and port.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <exception cref="SocketException">Connection failed.</exception>
        public void Connect(IPEndPoint endPoint)
        {
            /// TODO: implement error-handling.
            HostClient.BeginConnect(endPoint.Address, endPoint.Port, new AsyncCallback(delegate (IAsyncResult ar)
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

        public void Disconnect()
        {
            HostClient?.Close();
            HostClient = null;
            
            Console.WriteLine("Disconnected");
        }

        internal void Update()
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

        public void Dispose()
        {
            HostClient.Close();
        }
    }
}

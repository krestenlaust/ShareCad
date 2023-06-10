﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using ShareCad.Logging;

namespace ShareCad.Networking
{
    /// <summary>
    /// Temporarily appended 'Thing' to differentiate between this new class called NetworkManager and the old one.
    /// They have different functions.
    /// </summary>
    public class NetworkManager
    {
        public const short DefaultPort = 4040;
        public const int NetworkUpdateInterval = 100;

        public NetworkClient FocusedClient = null;
        readonly Logger log = new Logger("Manager", false);
        readonly Thread networkThread;
        readonly List<Server> servers = new List<Server>();
        readonly List<NetworkClient> clients = new List<NetworkClient>();
        short currentPort = DefaultPort;
        bool networkRunning = true;

        public NetworkManager()
        {
            networkThread = new Thread(NetworkLoop);
            networkThread.Start();
        }

        public short StartServer() => StartServer(IPAddress.Any);

        public short StartServer(IPAddress bindAddress)
        {
            short port = currentPort++;

            servers.Add(new Server(new IPEndPoint(bindAddress, port)));

            return port;
        }

        public NetworkClient InstantiateClient(IPEndPoint endpoint)
        {
            NetworkClient client = new NetworkClient(endpoint);

            clients.Add(client);

            return client;
        }

        /// <summary>
        /// Stops any client/server running.
        /// </summary>
        public void Stop()
        {
            networkRunning = false;
        }

        void NetworkLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (networkRunning)
            {
                stopwatch.Restart();

                // Send queued packets.
                foreach (var client in clients)
                {
                    client.SendAllQueuedPackets();
                }

                // Handle server logic.
                foreach (var server in servers)
                {
                    server.Update();
                }

                // Handle client logic.
                if (!(FocusedClient is null))
                {
                    if (FocusedClient.HostClient.Connected)
                    {
                        FocusedClient.Update();
                    }
                }

                while (stopwatch.ElapsedMilliseconds < NetworkUpdateInterval)
                    Thread.Sleep(0);
            }

            // Close servers.
            foreach (var server in servers)
            {
                server.Dispose();
            }
        }
    }
}

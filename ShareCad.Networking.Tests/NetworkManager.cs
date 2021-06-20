﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ShareCad.Networking.Tests
{
    /// <summary>
    /// Summary description for NetworkManager
    /// </summary>
    [TestClass]
    public class NetworkManager
    {
        [TestMethod]
        public void InitializeNetwork()
        {
            Networking.NetworkManager networkManager = new Networking.NetworkManager(NetworkFunction.Host);
            networkManager.Start(System.Net.IPAddress.Loopback);
        }
    }
}

﻿using Ptc.Controls;
using Ptc.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ShareCad.Core
{
    public class Core
    {
        readonly IList<SharedDocument> sharedDocuments = new List<SharedDocument>();

        public Core()
        {

        }

        public void ShareCadRibbon_ConnectToDocumentPressed()
        {
            InquireIP result = new InquireIP();

            var dlgResult = result.Show(WpfUtils.ApplicationFullName);
            if (dlgResult != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            NewDocumentIP = result.IP;
            NewDocumentPort = result.Port;
            NewDocumentAction = NetworkFunction.Guest;
            AppCommands.NewEngineeringDocument.Execute(null, SpiritMainWindow);
        }

        public void ShareCadRibbon_ShareNewDocumentPressed()
        {
            NewDocumentAction = NetworkFunction.Host;
            AppCommands.NewEngineeringDocument.Execute(null, SpiritMainWindow);
        }

        public void ShareCadRibbon_ShareCurrentDocumentPressed()
        {
            EngineeringDocument currentDocument = GetCurrentTabDocument();

            if (!(GetSharedDocumentByEngineeringDocument(currentDocument) is null))
            {
                Console.WriteLine("Already shared");
                return;
            }

            if (currentDocument is null)
            {
                return;
            }

            var sharedDoc = StartSharedDocument(currentDocument);
            NetworkManager.FocusedClient = sharedDoc.NetworkClient;
        }

        public void ShareCadRibbon_StopSharingPressed()
        {
            NetworkManager.Stop();
        }

        internal void DecoupleSharedDocument(SharedDocument sharedDocument)
        {
            sharedDocuments.Remove(sharedDocument);
        }

        SharedDocument GetSharedDocumentByEngineeringDocument(EngineeringDocument doc)
        {
            return (from document in sharedDocuments
                    where document.Document == doc
                    select document).FirstOrDefault();
        }

        SharedDocument StartSharedDocument(EngineeringDocument document)
        {
            if (NetworkManager is null)
            {
                NetworkManager = new NetworkManager();
            }

            int port = NetworkManager.StartServer();
            Log.Print($"Started server at port {port}");

            MessageBoxManager.ShowInfo($"Shared document on {GetLocalIPAddress()}:{port}", "IP Address and port");

            return ConnectSharedDocument(document, new IPEndPoint(IPAddress.Loopback, port));
        }

        SharedDocument ConnectSharedDocument(EngineeringDocument document, IPEndPoint targetEndpoint)
        {
            if (NetworkManager is null)
            {
                NetworkManager = new NetworkManager();
            }

            NetworkClient client = NetworkManager.InstantiateClient(targetEndpoint);

            client.OnConnectFinished += Client_OnConnectFinished;
            client.Connect();

            SharedDocument sharedDocument = new SharedDocument(document, client, Log);
            sharedDocuments.Add(sharedDocument);

            return sharedDocument;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using DevComponents.WpfDock;
using DevComponents.WpfRibbon;
using HarmonyLib;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Wpf;
using ShareCad.Logging;
using ShareCad.Networking;
using Spirit;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    [HarmonyPatch]
    public class ShareCad
    {
        enum Modes
        {
            ShareMode,
            LogWindow
        }

        public static ShareCad Instance;
        public static SpiritMainWindow SpiritMainWindow;
        public static IMessageBoxManager MessageBoxManager;

        public static readonly NetworkManager NetworkManager = new NetworkManager();
        public static NetworkFunction? NewDocumentAction = null;
        public static IPAddress NewDocumentIP = IPAddress.Loopback;
        public static int NewDocumentPort = NetworkManager.DefaultPort;
        /// <summary>
        /// Sand, når modulet er initializeret.
        /// </summary>
        private static bool initializedModule = false;
        private static readonly List<SharedDocument> sharedDocuments = new List<SharedDocument>();
        public static Logger Log = new Logger("", true);

        /// <summary>
        /// Initialisére harmony og andre funktionaliteter af sharecad.
        /// </summary>
        public void ShareCadInit()
        {
            if (initializedModule)
                return;

            // TODO: Hej Alexander, jeg gad godt have nogle command line arguments her.
            string[] args;
            // ...
            args = new string[] { "-share", "-log" };
            //args = Environment.GetCommandLineArgs();
            // ...
            
            List<Modes> modes = ParseCommandlineArguments(args);

            if (!modes.Contains(Modes.ShareMode))
            {
                Console.WriteLine("ShareCad disabled");
                return;
            }

            if (modes.Contains(Modes.LogWindow))
            {
                //WinConsole.Initialize();
            }

            Instance = this;
            SpiritMainWindow = (SpiritMainWindow)Application.Current.MainWindow;
            SpiritMainWindow.Closed += SpiritMainWindow_Closed;
            MessageBoxManager = (IMessageBoxManager)AccessTools.Property(typeof(SpiritMainWindow), "MessageBoxManager").GetValue(SpiritMainWindow);

            ((DockWindowGroup)AccessTools.Field(typeof(SpiritMainWindow), "MainDockWindowGroup").GetValue(SpiritMainWindow)).SelectionChanged += ShareCad_SelectionChanged;

            var harmony = new Harmony("ShareCad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ShareCadRibbonParts.ExtendRibbonControl(GetRibbon());

            Log.Print("LOADED!");
            initializedModule = true;
        }

        private void SpiritMainWindow_Closed(object sender, EventArgs e)
        {
            NetworkManager.Stop();
            Instance = null;
            SpiritMainWindow = null;
            Application.Current.Dispatcher.InvokeShutdown();
        }

        private List<Modes> ParseCommandlineArguments(string[] args)
        {
            List<Modes> modes = new List<Modes>();

            foreach (var item in args)
            {
                switch (item.ToLower())
                {
                    case "-share":
                        modes.Add(Modes.ShareMode);
                        break;
                    case "-log":
                        modes.Add(Modes.LogWindow);
                        break;
                    default:
                        break;
                }
            }

            return modes;
        }

        private void ShareCad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Print("Selection changed: " + sender + " . " + e.RoutedEvent);


            DockWindow dockWindow = e.AddedItems[0] as DockWindow;
            EngineeringDocument documentForTab = (EngineeringDocument)SpiritMainWindow.GetDocumentForTab(dockWindow);

            var sharedDocument = (from document in sharedDocuments
                        where document.Document == documentForTab
                        select document).FirstOrDefault();

            if (sharedDocument is null)
            {
                return;
            }

            NetworkManager.FocusedClient = sharedDocument.NetworkClient;
            sharedDocument.NetworkClient.Update();
        }

        /// <summary>
        /// Initialisére konkrete funktionaliteter forbundet med det nye dokument.
        /// </summary>
        /// <param name="__result"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })]
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            if (NewDocumentAction is null)
            {
                Log.Print("Normal document");
                return;
            }

            int port = NewDocumentPort;

            if (NewDocumentAction.Value == NetworkFunction.Host)
            {
                Log.Print("Host document");

                port = NetworkManager.StartServer();
                Log.Print($"Started server at port {port}");
            }
            else
            {
                Log.Print("Guest document");
            }

            EngineeringDocument engineeringDocument = (EngineeringDocument)__result;

            NetworkClient client = NetworkManager.InstantiateClient(new IPEndPoint(NewDocumentIP, port));
            NetworkManager.FocusedClient = client;

            client.OnConnectFinished += Client_OnConnectFinished;
            client.Connect();
            sharedDocuments.Add(new SharedDocument(engineeringDocument, client, Log));

            NewDocumentAction = null;
        }

        private static void Client_OnConnectFinished(NetworkClient.ConnectStatus obj)
        {
            Log.Print("Connection finished: " + obj);
        }

        private Ribbon GetRibbon()
        {
            ContentControl rootElement = (ContentControl)Application.Current.MainWindow;
            Panel panel = (Panel)rootElement.Content;
            return (Ribbon)panel.Children[0];
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action){}*/
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using DevComponents.WpfRibbon;
using HarmonyLib;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
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
        public static ShareCad Instance;

        public static NetworkFunction? NewDocumentAction = null;
        /// <summary>
        /// Sand, når modulet er initializeret.
        /// </summary>
        private static bool initializedModule = false;
        /// <summary>
        /// Sand, når de dokument-specifikke ting, såsom event listeners til dokumentet, er initialiseret.
        /// </summary>
        private static bool initializedDocument = false;
        //private static ControllerWindow controllerWindow;
        private static List<SharedDocument> sharedDocuments = new List<SharedDocument>();
        private static NetworkManagerThing networkmanagerThing = new NetworkManagerThing();
        public static Logger log = new Logger("", true);

        /// <summary>
        /// Initialisére harmony og andre funktionaliteter af sharecad.
        /// </summary>
        public void ShareCadInit()
        {
            if (initializedModule)
                return;

            Instance = this;

            var harmony = new Harmony("ShareCad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            WinConsole.Initialize();

            // opsæt vinduet til at styre delingsfunktionaliteten, men vis det først senere.
            //controllerWindow = new ControllerWindow();
            //controllerWindow.OnActivateShareFunctionality += SharecadControl_OnActivateShareFunctionality;
            //controllerWindow.FormClosing += (object _, System.Windows.Forms.FormClosingEventArgs e) => Environment.Exit(0);

            ShareCadRibbonParts.ExtendRibbonControl(GetRibbon());

            log.Print("LOADED!");
            initializedModule = true;
        }

        /// <summary>
        /// Initialisére konkrete funktionaliteter forbundet med det nye dokument.
        /// </summary>
        /// <param name="__result"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })]
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            if (initializedDocument)
                return;

            if (!NewDocumentAction.HasValue)
            {
                log.Print("Normal document");
                return;
            }

            short port = NetworkManagerThing.DefaultPort;

            if (NewDocumentAction.Value == NetworkFunction.Host)
            {
                log.Print("Host document");

                port = networkmanagerThing.StartServer();
                log.Print($"Started server at port {port}");
            }
            else
            {
                log.Print("Guest document");
            }

            EngineeringDocument engineeringDocument = (EngineeringDocument)__result;

            NetworkClient client = networkmanagerThing.InstantiateClient(new IPEndPoint(IPAddress.Loopback, port));
            networkmanagerThing.FocusedClient = client;

            client.OnConnectFinished += Client_OnConnectFinished;
            client.Connect();
            sharedDocuments.Add(new SharedDocument(engineeringDocument, client, log));

            // vis vinduet til at styre delingsfunktionaliteten.
            //controllerWindow.Show();

            // register keys
            //CommandManager.RegisterClassInputBinding(
            //    typeof(WorksheetControl), 
            //    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));

            NewDocumentAction = null;
            //initializedDocument = true;
        }

        private static void Client_OnConnectFinished(NetworkClient.ConnectStatus obj)
        {
            log.Print("Connection finished: " + obj);
        }

        private Ribbon GetRibbon()
        {
            ContentControl rootElement = (ContentControl)Application.Current.MainWindow;
            Panel panel = (Panel)rootElement.Content;
            return (Ribbon)panel.Children[0];
        }

        /*
        private static void Worksheet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            log.Print($"PropertyChange invoked - {e.PropertyName}");

            //WorksheetControl control = (WorksheetControl)sender;
            //var worksheetData = control.GetWorksheetData();

            //var viewModel = control.GetViewModel();

            //Console.WriteLine($" - ActiveItem: {control.ActiveItem}, {control.ActiveDescendant}, {control.CurrentElement}");
            // for at finde ud af hvad der gør dem unik så man kan sende et ID med over nettet.
            //Console.WriteLine($"ID: {control.PersistId}");

            // Liste over aktive elementer.
            //Console.WriteLine(" - Active section items:");
            //foreach (var item in worksheetData.WorksheetContent.RegionsToSerialize)
            //{
            //    Console.WriteLine($"{item.Key}");
            //}

            switch (e.PropertyName)
            {
                case "SelectedDescendants":
                    #region Testing
                    // finder det første element lavet, eller null.
                    //var firstElement = control.ActiveSectionItems.FirstOrDefault();

                    // aktivér debug test scenarie hvis der laves en tekstboks som det første element.
                    //if (firstElement is TextRegion realText)
                    //{
                    //Console.WriteLine("First element is text");

                    // flyt det første element til koordinatet (0, 5)
                    //control.MoveItemGridLocation(firstElement, new Point(0, 2));

                    //realText.Text = "👑〖⚡ᖘ๖ۣۜℜΘ𝕵ECT ΘVERRIDE⚡〗👑";

                    // Prøv at oprette et tekst element, (der bliver ikke gjort mere ved det lige nu).
                    //Ptc.Controls.Text.TextRegion textRegion = new Ptc.Controls.Text.TextRegion()
                    //{
                    //    Text = "INJECTED!",
                    //};

                    // Indsæt tekst element.
                    //viewModel.AddItemAtLocation(textRegion, viewModel.GridLocationToWorksheetLocation(new Point(5, 7)));

                    // Profit! (andre test ting)
                    //if (worksheetData is null)
                    //{
                    //    break;
                    //}
                    //
                    //using (Stream xmlStream = SerializeRegions(worksheetData.WorksheetContent))
                    //{
                    //    Networking.Networking.TransmitStream(xmlStream);
                    //}

                    //TcpClient client = new TcpClient("192.168.2.215", 8080);
                    //var tcpStream = client.GetStream();
                    //Networking.Networking.Server.BindListener(IPAddress.Loopback);
                    //}
                    //else if (firstElement is SolveBlockControl solveBlock)
                    //{
                    //    Console.WriteLine("First element is solveblock");
                    //    Networking.Networking.Client.Connect(IPAddress.Loopback);
                    //}
                    #endregion
                    break;
                case "CurrentElement":
                    break;
                case "WorksheetPageLayoutMode":
                    // changed from draft to page
                    break;
                case "IsCalculating":
                    networkPushDebounce.Interval = Update_DebounceTimeout;
                    networkPushDebounce.Start();
                    break;
                default:
                    break;
            }
        }*/

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action){}*/
    }
}

//if (previousDocument is null)
//{
//    previousDocument = xml;
//}
//else
//{
//    XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder |
//                        XmlDiffOptions.IgnoreNamespaces |
//                        XmlDiffOptions.IgnorePrefixes);

//    XmlDocument newXml = new XmlDocument();

//    StringBuilder sb = new StringBuilder();
//    xmldiff.Compare(previousDocument, xml, XmlWriter.Create(sb));

//    Console.WriteLine(sb);
//}
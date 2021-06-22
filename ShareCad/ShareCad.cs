﻿using HarmonyLib;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using ShareCad.Logging;
using Spirit;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Timers;
using System.Xml;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    [HarmonyPatch]
    public class ShareCad
    {
        private const string UploadIcon = "";//@"C:\Users\kress\source\repos\ShareCad\ShareCad\Resources\upload_icon.png";
        private const string DownloadIcon = "";//@"C:\Users\kress\source\repos\ShareCad\ShareCad\Resources\download_icon.png";
        /// <summary>
        /// The least amount of time passing, from the worksheet is changed to the server is notified.
        /// </summary>
        private const double Update_DebounceTimeout = 200;

        private static EngineeringDocument engineeringDocument;
        /// <summary>
        /// Sand, når modulet er initializeret.
        /// </summary>
        private static bool initializedModule = false;
        /// <summary>
        /// Sand, når de dokument-specifikke ting, såsom event listeners til dokumentet, er initialiseret.
        /// </summary>
        private static bool initializedDocument = false;
        private static ControllerWindow controllerWindow;

        private static Networking.NetworkManager networkManager;
        private static Timer networkPushDebounce = new Timer();
        private static bool ignoreFirstNetworkPush = true;
        private static Logger logger = new Logger("", true);

        /// <summary>
        /// Initialisére harmony og andre funktionaliteter af sharecad.
        /// </summary>
        public void ShareCadInit()
        {
            if (initializedModule)
                return;

            initializedModule = true;

            var harmony = new Harmony("ShareCad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            WinConsole.Initialize();

            // opsæt vinduet til at styre delingsfunktionaliteten, men vis det først senere.
            controllerWindow = new ControllerWindow();
            controllerWindow.OnActivateShareFunctionality += SharecadControl_OnActivateShareFunctionality;
            controllerWindow.FormClosing += (object _, System.Windows.Forms.FormClosingEventArgs e) => Environment.Exit(0);

            logger.Log("LOADED!");
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

            logger.Log("Retrieving document instance.");
            engineeringDocument = (EngineeringDocument)__result;

            engineeringDocument.Worksheet.PropertyChanged += Worksheet_PropertyChanged;

            engineeringDocument.DocumentTabIcon = @"C:\Users\kress\source\repos\ShareCad\ShareCad\Resources\upload_icon.png";
            logger.Log("DocumentTabIcon: " + engineeringDocument.DocumentTabIcon);
            logger.Log("CustomizedClosePrompt: " + engineeringDocument.CustomizedClosePrompt);

            // vis vinduet til at styre delingsfunktionaliteten.
            controllerWindow.Show();

            // register keys
            //CommandManager.RegisterClassInputBinding(
            //    typeof(WorksheetControl), 
            //    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));

            initializedDocument = true;
        }

        private void Sharecad_Push()
        {
            logger.Log("Pushing start");
            var worksheetData = engineeringDocument.Worksheet.GetWorksheetData();

            if (worksheetData is null)
            {
                engineeringDocument.DocumentTabIcon = "";
                return;
            }

            var regionsToSerialize = worksheetData.WorksheetContent.RegionsToSerialize;

            XmlDocument xml = ManipulateWorksheet.SerializeRegions(regionsToSerialize, engineeringDocument);
            
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

            /// TODO: transmit data.
            networkManager.SendDocument(xml);
            logger.Log("Pushing end");
            engineeringDocument.DocumentTabIcon = "";
        }

        private void SharecadControl_OnActivateShareFunctionality(Networking.NetworkFunction networkRole, IPAddress guestTargetIPAddress)
        {
            networkManager = new Networking.NetworkManager(networkRole);

            if (networkRole == Networking.NetworkFunction.Guest)
            {
                networkManager.Start(guestTargetIPAddress);
            }
            else
            {
                networkManager.Start(IPAddress.Any);
            }

            networkManager.OnWorksheetUpdate += UpdateWorksheet;

            networkPushDebounce.Elapsed += (object source, ElapsedEventArgs e) =>
            {
                networkPushDebounce.Stop();

                if (ignoreFirstNetworkPush)
                {
                    logger.Log("Push ignored");
                    ignoreFirstNetworkPush = false;
                    engineeringDocument.DocumentTabIcon = "";
                    return;
                }

                engineeringDocument.Dispatcher.Invoke(() =>
                {
                    Sharecad_Push();
                });
            };
        }

        private void UpdateWorksheet(XmlDocument doc)
        {
            engineeringDocument.Dispatcher.Invoke(() =>
            {
                engineeringDocument.DocumentTabIcon = DownloadIcon;
                ignoreFirstNetworkPush = true;
                
                IWorksheetViewModel viewModel = engineeringDocument._worksheet.GetViewModel();

                var worksheetItems = viewModel.WorksheetItems;
                if (worksheetItems.Count > 0)
                {
                    viewModel.SelectItems(viewModel.WorksheetItems);
                    viewModel.HandleBackspace();
                }

                ManipulateWorksheet.DeserializeAndApplySection(engineeringDocument, doc.OuterXml);
                engineeringDocument.DocumentTabIcon = "";

                logger.Log("Your worksheet has been updated");
            });
        }

        private static void Worksheet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            logger.Log($"PropertyChange invoked - {e.PropertyName}");

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
                    engineeringDocument.DocumentTabIcon = UploadIcon;
                    break;
                default:
                    break;
            }
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action){}*/

    }
}

using HarmonyLib;
using Ptc.Controls;
using Ptc.Controls.Core;
using Spirit;
using System;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Xml;
 
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    [HarmonyPatch]
    public class ShareCad
    {
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
            controllerWindow.OnSyncPull += SharecadControl_OnSyncPull;
            controllerWindow.OnSyncPush += SharecadControl_OnSyncPush;
            controllerWindow.FormClosing += (object _, System.Windows.Forms.FormClosingEventArgs e) => Environment.Exit(0);

            Console.WriteLine("LOADED!");
        }

        // Fra MathcadPrime.exe
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpiritMainWindow), "NewDocument", new Type[] { typeof(bool), typeof(DocumentReadonlyOptions), typeof(bool) })]
        public static void Postfix_SpiritMainWindow(ref IEngineeringDocument __result)
        {
            if (initializedDocument)
                return;

            Console.WriteLine("Retrieving document instance.");
            engineeringDocument = (EngineeringDocument)__result;

            engineeringDocument.Worksheet.PropertyChanged += Worksheet_PropertyChanged;

            // vis vinduet til at styre delingsfunktionaliteten.
            controllerWindow.Show();

            // register keys
            //CommandManager.RegisterClassInputBinding(
            //    typeof(WorksheetControl), 
            //    new InputBinding(new InputBindingFunctionalityCommandWrapper(WorksheetCommands.ToggleShowGrid), Gestures.CtrlUp));

            initializedDocument = true;
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WpfUtils), "ExecuteOnLayoutUpdated")]
        public static void Postfix_WpfUtils(ref UIElement element, ref Action action){}*/

        private void SharecadControl_OnSyncPush()
        {
            var worksheetData = engineeringDocument.Worksheet.GetWorksheetData();

            if (worksheetData is null)
            {
                return;
            }

            XmlDocument xml = ManipulateWorksheet.SerializeRegions(engineeringDocument, worksheetData.WorksheetContent);

            Networking.Transmit(xml.OuterXml);
        }

        private void SharecadControl_OnSyncPull()
        {
            if (Networking.ReceiveXml(out string readXml))
            {
                Console.WriteLine("Incoming data.");
                ManipulateWorksheet.DeserializeAndApplySection(engineeringDocument, readXml);
            }
            else
            {
                Console.WriteLine("No incoming data.");
            }
        }

        private void SharecadControl_OnActivateShareFunctionality(ControllerWindow.NetworkRole networkRole)
        {
            switch (networkRole)
            {
                case ControllerWindow.NetworkRole.Guest:
                    Networking.Client.Connect(IPAddress.Loopback);
                    break;
                case ControllerWindow.NetworkRole.Host:
                    Networking.Server.BindListener(IPAddress.Any);
                    break;
            }
        }

        private static void Worksheet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"=== {DateTime.Now:HH:mm:ss} - PropertyChange invoked - {e.PropertyName} ===");
            Console.ForegroundColor = ConsoleColor.Gray;

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

            /*
            Console.WriteLine();

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
                default:
                    break;
            }*/
        }
    }
}

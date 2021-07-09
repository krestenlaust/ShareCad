using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using Ptc.Wpf;
using ShareCad.Networking;

namespace ShareCad
{
    /// <summary>
    /// A SharedDocument reference is associated with an EngineeringDocument instance.
    /// If the SharedDocument is destructed (either by scope-loss or by dereferenciation) the connection is closed.
    /// </summary>
    public class SharedDocument
    {
        /// <summary>
        /// The least amount of time passing, from the worksheet is changed to the server is notified.
        /// </summary>
        private const double Update_DebounceTimeout = 200;

        public readonly EngineeringDocument Document;
        private readonly IWorksheetViewModel viewModel;

        public readonly NetworkClient NetworkClient;
        private readonly Logging.Logger log;
        private readonly Timer networkPushDebounce = new Timer();
        private bool ignoreFirstNetworkPush = true;

        /// <summary>
        /// Updates visual indicators of connection, and returns the connection status.
        /// </summary>
        private bool isConnected
        {
            get
            {
                if (NetworkClient.HostClient.Connected != previousConnectedStatus)
                {
                    UpdateConnectionStatus(NetworkClient.HostClient.Connected);
                }
                previousConnectedStatus = NetworkClient.HostClient.Connected;

                return previousConnectedStatus;
            }
        }
        private bool previousConnectedStatus;

        /// <summary>          
        /// Hooks the EngineeringDocument to enable sharing functionality.
        /// Can be initialized both before and after established connection.
        /// </summary>
        /// <param name="engineeringDocument"></param>
        public SharedDocument(EngineeringDocument engineeringDocument, NetworkClient client, Logging.Logger logger)
        {
            log = logger;

            NetworkClient = client;

            Document = engineeringDocument;
            viewModel = engineeringDocument.GetViewModel();

            Document.Worksheet.PropertyChanged += PropertyChanged;
            Document.KeyUp += MousePositionMightveChanged;
            Document.MouseMove += MousePositionMightveChanged;
            
            client.OnWorksheetUpdate += (doc) => Document.Dispatcher.Invoke(() => UpdateWorksheet(doc));
            client.OnCollaboratorCursorUpdate += UpdateLocalCursorPosition;

            networkPushDebounce.Stop();
            networkPushDebounce.Elapsed += delegate (object source, ElapsedEventArgs e)
            {
                networkPushDebounce.Stop();

                if (ignoreFirstNetworkPush)
                {
                    log.Print("Push ignored");
                    ignoreFirstNetworkPush = false;
                    return;
                }

                engineeringDocument.Dispatcher.Invoke(() => PushDocument());
            };

            Document.Dispatcher.Invoke(() =>
            {
                Document.DocumentName = client.Endpoint.ToString();
                Document.DocumentTabIcon = @"C:\Program Files\Lenovo\Nerve Center\TaskbarSkin\ResPath_black\GZMenu\btn_discover_loading.gif";

                if (NetworkClient.isConnecting)
                {
                    NetworkClient.OnConnectFinished += (_) => Document.Dispatcher.Invoke(() => UpdateConnectionStatus(NetworkClient.HostClient.Connected));
                }
                else
                {
                    UpdateConnectionStatus(NetworkClient.HostClient.Connected);
                }
            });
        }

        /// <summary>
        /// Unhooks document.
        /// </summary>
        public void StopSharing()
        {
            Document.Worksheet.PropertyChanged -= PropertyChanged;
            Document.KeyUp -= MousePositionMightveChanged;

            Document.Dispatcher.Invoke(() =>
            {
                Document.DocumentTabIcon = "";
            });
        }

        /// <summary>
        /// Requires dispatching.
        /// </summary>
        /// <param name="connected"></param>
        private void UpdateConnectionStatus(bool connected)
        {
            Document.DocumentTabIcon = System.IO.Path.GetFullPath(connected ? BootlegResourceManager.Icons.ConnectIcon : BootlegResourceManager.Icons.NoConnectionIcon);
        }

        private void PushDocument()
        {
            if (!isConnected)
            {
                log.Print("Not connected");
                return;
            }

            var worksheetData = Document.Worksheet.GetWorksheetData();

            if (worksheetData is null)
            {
                return;
            }

            var regionsToSerialize = worksheetData.WorksheetContent.RegionsToSerialize;

            XmlDocument xml = ManipulateWorksheet.SerializeRegions(regionsToSerialize, Document);

            // TODO: transmit data.
            NetworkClient.SendDocument(xml);
            log.Print("Pushed document");
        }

        /// <summary>
        /// Requires dispatching.
        /// </summary>
        /// <param name="doc"></param>
        private void UpdateWorksheet(XmlDocument doc)
        {
            ignoreFirstNetworkPush = true;

            // den region man skriver i lige nu.
            var currentItem = viewModel.ActiveItem;

            var worksheetItems = viewModel.WorksheetItems;
            if (worksheetItems.Count > 0)
            {
                Point previousPosition = viewModel.InsertionPoint;

                // Select every item on whiteboard.
                viewModel.HandleSelectAll();
                //documentViewModel.SelectItems(documentViewModel.WorksheetItems);

                // Deselect current item if any.
                if (!(currentItem is null))
                {
                    viewModel.ToggleItemSelection(currentItem, true);
                }

                // Delete all selected items.
                viewModel.HandleBackspace();

                // Change insertion point back to previous position.
                //documentViewModel.InsertionPoint = previousPosition;
            }

            ManipulateWorksheet.DeserializeAndApplySection(Document, doc.OuterXml);

            log.Print("Your worksheet has been updated");
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsCalculating")
            {
                // refresh connected status.
                if (isConnected)
                {
                    networkPushDebounce.Interval = Update_DebounceTimeout;
                    networkPushDebounce.Start();
                }
            }
        }

        /// <summary>
        /// Stops sharing.
        /// </summary>
        ~SharedDocument()
        {
            StopSharing();
        }

        private void MousePositionMightveChanged(object sender, System.Windows.Input.InputEventArgs e)
        {
            RemoteUpdateCursorPosition();
        }

        private Point previousCursorPosition;

        public void RemoteUpdateCursorPosition()
        {
            if (NetworkClient is null)
            {
                return;
            }

            if (previousCursorPosition == Document.InsertionPoint)
            {
                return;
            }

            NetworkClient.SendCursorPosition(Document.InsertionPoint);
            previousCursorPosition = Document.InsertionPoint;
        }

        private readonly Dictionary<byte, CollaboratorCrosshair> collaboratorCrosshairs = new Dictionary<byte, CollaboratorCrosshair>();

        public void UpdateLocalCursorPosition(byte ID, Point position)
        {
            // TODO: Implement visual for other cursors.

            Document.Dispatcher.Invoke(() =>
            {
                CollaboratorCrosshair crosshair;
                if (!collaboratorCrosshairs.TryGetValue(ID, out crosshair))
                {
                    crosshair = new CollaboratorCrosshair(viewModel);
                    collaboratorCrosshairs[ID] = crosshair;
                }

                crosshair.MoveCrosshair(position);
            });
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

        /// <summary>
        /// Manages collaborator crosshairs. One crosshair per page. Currently only supports non-draft mode.
        /// </summary>
        private class CollaboratorCrosshair
        {
            // Det er vel teknisk set 50, men tror 49 er det rigtige for det den bruges til.
            private const int PageGridHeight = 49;

            /// <summary>
            /// Crosshair instances by their page index.
            /// </summary>
            private readonly Dictionary<int, Crosshair> crosshairInstances = new Dictionary<int, Crosshair>();
            private readonly SolidColorBrush crosshairColor = Brushes.Green;
            private readonly IWorksheetViewModel viewModel;
            private int previousPage;

            public CollaboratorCrosshair(IWorksheetViewModel viewModel)
            {
                this.viewModel = viewModel;
            }

            public CollaboratorCrosshair(IWorksheetViewModel viewModel, SolidColorBrush crosshairColor)
            {
                this.viewModel = viewModel;
                this.crosshairColor = crosshairColor;
            }

            public void MoveCrosshair(Point newPosition)
            {
                int newPageIndex = GetPageByPosition(newPosition);

                // if a crosshair has been instantiated there. It doesn't matter if the page is the same as the new page,
                // it is shown latter anyway.
                if (crosshairInstances.TryGetValue(previousPage, out Crosshair previousCrosshair))
                {
                    previousCrosshair.Visibility = Visibility.Collapsed;
                }

                Crosshair targetCrosshair;
                if (!crosshairInstances.TryGetValue(newPageIndex, out targetCrosshair))
                {
                    // Crosshair doesn't exist, instantiate a new one.
                    targetCrosshair = InstantiateCrosshairOnPage(newPageIndex);

                    // returns if the page doesn't exist.
                    if (targetCrosshair is null)
                    {
                        return;
                    }
                }

                // move target crosshair and make sure it is displayed properly.
                double yOffset = viewModel.GridLocationToWorksheetLocation(new Point(0, PageGridHeight)).Y * newPageIndex;
                Canvas.SetLeft(targetCrosshair, newPosition.X);
                Canvas.SetTop(targetCrosshair, newPosition.Y - yOffset);

                if (targetCrosshair.IsLoaded)
                {
                    targetCrosshair.Visibility = Visibility.Visible;
                }
                else
                {
                    targetCrosshair.Loaded += delegate { targetCrosshair.Visibility = Visibility.Visible; };
                }

                previousPage = newPageIndex;
            }

            // TODO: ikke lavet endnu, lige nu laver den bare et crosshair på den første side.
            private Crosshair InstantiateCrosshairOnPage(int pageIndex)
            {
                IEnumerable<IWorksheetPage> pages = viewModel.PageManager.Pages;

                // page doesn't exist.
                if (pages.Count() <= pageIndex)
                {
                    return null;
                }

                WorksheetPageBody worksheetPageBody = (WorksheetPageBody)pages.ElementAt(pageIndex).PageBody;

                PageBodyCanvas pageBodyCanvas = (PageBodyCanvas)worksheetPageBody.Content;
                
                Crosshair crosshair = new Crosshair();
                Line line1 = (Line)crosshair.Children[0];
                Line line2 = (Line)crosshair.Children[1];
                line1.Stroke = crosshairColor;
                line2.Stroke = crosshairColor;

                crosshairInstances[pageIndex] = crosshair;
                pageBodyCanvas.Children.Add(crosshair);

                return crosshair;
            }


            /// <summary>
            /// Gets the page containing the crosshair.
            /// </summary>
            /// <returns></returns>
            private int GetPageByGridPosition(Point gridPosition)
            {
                return (int)(gridPosition.Y / PageGridHeight);
            }

            private int GetPageByPosition(Point worksheetPosition)
            {
                Point gridPosition = viewModel.WorksheetLocationToGridLocation(worksheetPosition);
                return GetPageByGridPosition(gridPosition);
            }
        }
    }
}

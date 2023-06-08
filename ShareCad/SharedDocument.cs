using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using Ptc.PersistentData;
using Ptc.PersistentDataObjects;
using Ptc.Serialization;
using Ptc.Wpf;
using ShareCad.Networking;
using ShareCad.Networking.Packets;
using Path = System.IO.Path;

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
        const double Update_DebounceTimeoutCalculation = 200;
        const double Update_DebounceTimeoutKeypress = 700;

        public readonly EngineeringDocument Document;
        public readonly NetworkClient NetworkClient;

        readonly Dictionary<byte, CollaboratorCrosshair> collaboratorCrosshairs = new Dictionary<byte, CollaboratorCrosshair>();
        readonly Dictionary<int, string> fileByID = new Dictionary<int, string>();
        readonly IWorksheetViewModel viewModel;
        readonly Logging.Logger log;
        readonly Timer networkPushDebounce = new Timer();
        bool ignoreFirstNetworkPush = false;
        bool ignoreNextCalculateChange = false;
        Point previousCursorPosition;
        bool previousConnectedStatus;
        readonly CustomMcdxSerializer customMcdxSerializer;

        /// <summary>
        /// Updates visual indicators of connection, and returns the connection status.
        /// </summary>
        bool isConnected
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

            customMcdxSerializer = new CustomMcdxSerializer(Document.DocumentSerializationHelper, StoreAsset);

            Document.Worksheet.PropertyChanged += PropertyChanged;
            Document.KeyUp += MousePositionMightveChanged;
            Document.MouseMove += MousePositionMightveChanged;
            Document.OnDispose += Document_OnDispose;

            client.OnWorksheetUpdate += (doc) => Document.Dispatcher.Invoke(() => UpdateWorksheet(doc));
            client.OnCollaboratorCursorUpdate += UpdateLocalCursorPosition;
            client.OnDisconnected += () => Document.Dispatcher.Invoke(() => UpdateConnectionStatus(false));
            client.OnXamlPackageUpdate += Client_OnXamlPackageUpdate;

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
                Document.DocumentName = Document.DocumentName + "(delt)";
                Document.CustomizedDocumentName = client.Endpoint.ToString();

                if (NetworkClient.isConnecting)
                {
                    NetworkClient.OnConnectFinished += (_) => Document.Dispatcher.Invoke(() => UpdateConnectionStatus(NetworkClient.HostClient.Connected));
                }
                else
                {
                    UpdateConnectionStatus(NetworkClient.HostClient.Connected);
                }
            });
            
            RemoteUpdateCursorPosition();
        }

        /// <summary>
        /// Unhooks document.
        /// </summary>
        public void StopSharing()
        {
            NetworkClient?.DisconnectClient();
            ShareCad.DecoupleSharedDocument(this);

            if (Document is null)
            {
                return;
            }

            Document.Worksheet.PropertyChanged -= PropertyChanged;
            Document.KeyUp -= MousePositionMightveChanged;

            Document.Dispatcher.Invoke(() =>
            {
                Document.DocumentTabIcon = "";
            });
        }

        public Stream LoadAsset(int id)
        {
            log.Print($"Loaded asset with ID {id}");

            if (!fileByID.TryGetValue(id, out string filepath))
            {
                return null;
            }

            try
            {
                return File.Open(filepath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                log.PrintError(ex);
            }

            return null;
        }

        public void NotifyStoreAsset(int id, string filePath)
        {
            log.Print("Notified");

            // Store asset
            StoreAsset(id, filePath);

            // Open file for streaming
            byte[] assetData = File.ReadAllBytes(filePath);

            var packet = new SerializedXamlPackageUpdate(id, assetData);
            NetworkClient.SendAsset(packet);
        }

        public void Client_OnXamlPackageUpdate(int id, byte[] serializedXamlPackage)
        {
            // Save serialized package to file in temp folder.
            string filepath = Path.GetTempFileName();

            using (Stream stream = File.Open(filepath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                stream.Write(serializedXamlPackage, 0, serializedXamlPackage.Length);
                stream.Flush();
            }

            StoreAsset(id, filepath);
        }

        void StoreAsset(int id, string filePath)
        {
            log.Print($"Stored asset with ID {id} at {filePath}");
            if (fileByID.TryGetValue(id, out string oldFilePath))
            {
                try
                {
                    File.Delete(oldFilePath);
                }
                catch (Exception ex)
                {
                    log.PrintError(ex);
                }
            }

            fileByID[id] = filePath;
        }

        void Document_OnDispose(IEngineeringDocument obj)
        {
            StopSharing();
        }

        /// <summary>
        /// Requires dispatching.
        /// </summary>
        /// <param name="connected"></param>
        void UpdateConnectionStatus(bool connected)
        {
            Document.DocumentTabIcon = Path.GetFullPath(connected ? BootlegResourceManager.Icons.ConnectIcon : BootlegResourceManager.Icons.NoConnectionIcon);
        }

        void PushDocument()
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

            XmlDocument xml = SerializeRegions(regionsToSerialize, Document);

            // TODO: transmit data.
            NetworkClient.SendDocument(xml);
            log.Print("Pushed document");
        }

        /// <summary>
        /// Requires dispatching.
        /// </summary>
        /// <param name="doc"></param>
        void UpdateWorksheet(XmlDocument doc)
        {
            ignoreFirstNetworkPush = true;

            (IList<IRegionPersistentData> regionData, IWorksheetPersistentData worksheetData) = Deserialize(Document, doc);

            // den region man skriver i lige nu.
            var currentItem = viewModel.ActiveItem;

            var worksheetItems = viewModel.WorksheetItems;
            if (worksheetItems.Count > 0)
            {
                Point previousPosition = viewModel.InsertionPoint;
                
                // Select every item on whiteboard.
                viewModel.HandleSelectAll();

                //viewModel.SelectItem(regionData.First());

                // Deselect controls that aren't capable of being shared.
                foreach (var item in viewModel.ActiveOrSelectedItems)
                {
                    /*
                    if (item is Ptc.Controls.Text.TextRegion)
                    {
                        viewModel.ToggleItemSelection(item, false);
                    }*/
                }

                // Deselect current item if any.
                if (!(currentItem is null))
                {
                    viewModel.ToggleItemSelection(currentItem, true);
                }

                // Delete all selected items.
                viewModel.HandleBackspace();

                // Change insertion point back to previous position.
                viewModel.InsertionPoint = previousPosition;
            }

            ignoreNextCalculateChange = true;

            ApplyMainSection(Document, regionData, worksheetData);

            log.Print("Your worksheet has been updated");
        }

        Control previousElement;

        void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            log.Print(e.PropertyName);

            if (e.PropertyName == "CurrentElement")
            {
                WorksheetControl senderControl = (WorksheetControl)sender;
                Control currentElement = senderControl.ActiveItem as Control;

                if (previousElement is null)
                {
                    if (!(currentElement is null))
                    {
                        currentElement.KeyUp += NewElement_KeyUp;
                    }
                }
                else if (currentElement is null && !(previousElement is null))
                {
                    previousElement.KeyUp -= NewElement_KeyUp;
                }

                previousElement = currentElement;
            }

            
            if (e.PropertyName == "IsCalculating")
            {
                if (ignoreNextCalculateChange)
                {
                    ignoreNextCalculateChange = false;
                    log.Print("Ignored calculate update");
                    return;
                }

                // refresh connected status.
                if (isConnected)
                {
                    networkPushDebounce.Interval = Update_DebounceTimeoutCalculation;
                    networkPushDebounce.Start();
                }
            }
        }

        void NewElement_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            log.Print(e.Key);

            if (e.Key == System.Windows.Input.Key.Insert)
            {
                throw new Exception();
            }

            if (isConnected)
            {
                //networkPushDebounce.Interval = Update_DebounceTimeoutKeypress;
                //networkPushDebounce.Start();
            }
        }

        void MousePositionMightveChanged(object sender, System.Windows.Input.InputEventArgs e)
        {
            RemoteUpdateCursorPosition();
        }

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

        public void UpdateLocalCursorPosition(byte ID, Point position, bool destroyCursor)
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

                if (destroyCursor)
                {
                    crosshair.Destroy();
                    collaboratorCrosshairs.Remove(ID);
                    return;
                }

                crosshair.MoveCrosshair(position);
            });
        }

        (IList<IRegionPersistentData>, IWorksheetPersistentData) Deserialize(EngineeringDocument currentDocument, XmlDocument xmlDoc)
        {
            if (currentDocument is null)
            {
                throw new ArgumentNullException(nameof(currentDocument));
            }

            var currentWorksheetData = currentDocument.Worksheet.GetWorksheetData();

            IRegionCollectionSerializer regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            IWorksheetPersistentData worksheetData = new WorksheetPersistentData()
            {
                DisplayGrid = currentWorksheetData.DisplayGrid,
                DisplayHFGrid = currentWorksheetData.DisplayHFGrid,
                GridSize = currentWorksheetData.GridSize,
                LayoutSize = currentWorksheetData.LayoutSize,
                MarginType = currentWorksheetData.MarginType,
                OleObjectAutoResize = currentWorksheetData.OleObjectAutoResize,
                PageOrientation = currentWorksheetData.PageOrientation,
                PlotBackgroundType = currentWorksheetData.PlotBackgroundType,
                ShowIOTags = currentWorksheetData.ShowIOTags
            };

            CustomMcdxDeserializer mcdxDeserializer = new CustomMcdxDeserializer(
                    new CustomWorksheetSectionDeserializationStrategy(
                        worksheetData.WorksheetContent,
                        currentDocument.MathFormat,
                        currentDocument.LabeledIdFormat
                        ),
                    currentDocument.DocumentSerializationHelper,
                    regionCollectionSerializer,
                    true,
                    LoadAsset
                    );

            mcdxDeserializer.Deserialize(xmlDoc);
            return (mcdxDeserializer.DeserializedRegions, worksheetData);
        }

        void ApplyMainSection(EngineeringDocument engineeringDocument, IList<IRegionPersistentData> regionsToApply, IWorksheetPersistentData worksheetData)
        {
            engineeringDocument.DocumentSerializationHelper.MainRegions = regionsToApply;

            WorksheetControl worksheetControl = (WorksheetControl)engineeringDocument.Worksheet;
            worksheetControl.ApplyWorksheetDataLite(worksheetData);
        }

        XmlDocument SerializeRegions(IDictionary<UIElement, Point> serializableRegions, ISerializationHelper serializationHelper)
        {
            var regionCollectionSerializer = new worksheetRegionCollectionSerializer();

            // Assign ID's to regions
            if (serializationHelper.GetNextRegionId() != 0)
            {
                serializationHelper.Reset();
            }

            Dictionary<UIElement, long> regionIDs = new Dictionary<UIElement, long>();
            foreach (var element in serializableRegions.Keys)
            {
                long id = serializationHelper.GetRegionId(element);

                if (id == 0)
                {
                    serializationHelper.AssignNextRegionId(element);
                    id = serializationHelper.GetRegionId(element);
                }

                regionIDs[element] = id;
            }

            /*
            var mcdxSerializer = new CustomMcdxSerializer(
                new CustomWorksheetSectionSerializationStrategy(
                    serializableRegions,
                    () => new worksheetRegionType()
                    ),
                serializationHelper,
                regionCollectionSerializer,
                null,
                true,
                NotifyStoreAsset);
            */

            var serializationStrategy = new CustomWorksheetSectionSerializationStrategy(serializableRegions, () => new worksheetRegionType());

            //serializationHelper.Reset();
            customMcdxSerializer.Serialize(regionCollectionSerializer, serializationStrategy);

            return customMcdxSerializer.XmlContentDocument;
        }

        object GetRegionData(UIElement control)
        {
            if (control is IPersistentDataProvider dataProvider)
            {
                return dataProvider.GetRegionPersistentData(customMcdxSerializer);
            }

            return null;
        }

        XmlDocument SerializeRegions(IDictionary<UIElement, Point> serializableRegions, EngineeringDocument engineeringDocument) =>
            SerializeRegions(serializableRegions, engineeringDocument.DocumentSerializationHelper);

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
        class CollaboratorCrosshair
        {
            // Det er vel teknisk set 50, men tror 49 er det rigtige for det den bruges til.
            const int PageGridHeight = 49;

            /// <summary>
            /// Crosshair instances by their page index.
            /// </summary>
            readonly Dictionary<int, Crosshair> crosshairInstances = new Dictionary<int, Crosshair>();
            readonly SolidColorBrush crosshairColor = Brushes.Green;
            readonly IWorksheetViewModel viewModel;
            int previousPage;

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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pageIndex"></param>
            /// <returns>Null if page index is bigger than actual number of pages.</returns>
            PageBodyCanvas GetPageBodyCanvas(int pageIndex)
            {
                IEnumerable<IWorksheetPage> pages = viewModel.PageManager.Pages;

                int pageCount = pages.Count();
                if (pageCount <= pageIndex)
                {
                    return null;
                }

                WorksheetPageBody pageBody = (WorksheetPageBody)pages.ElementAt(pageIndex).PageBody;

                return (PageBodyCanvas)pageBody.Content;
            }

            // TODO: ikke lavet endnu, lige nu laver den bare et crosshair på den første side.
            Crosshair InstantiateCrosshairOnPage(int pageIndex)
            {
                PageBodyCanvas canvas = GetPageBodyCanvas(pageIndex);

                if (canvas is null)
                {
                    return null;
                }
                
                Crosshair crosshair = new Crosshair();
                Line line1 = (Line)crosshair.Children[0];
                Line line2 = (Line)crosshair.Children[1];
                line1.Stroke = crosshairColor;
                line2.Stroke = crosshairColor;

                crosshairInstances[pageIndex] = crosshair;
                canvas.Children.Add(crosshair);

                return crosshair;
            }
            
            void RemoveCrosshairOnPage(Crosshair crosshair, int pageIndex)
            {
                if (crosshair is null)
                {
                    return;
                }

                PageBodyCanvas canvas = GetPageBodyCanvas(pageIndex);

                if (canvas is null)
                {
                    return;
                }

                canvas.Children.Remove(crosshair);
            }


            /// <summary>
            /// Gets the page containing the crosshair.
            /// </summary>
            /// <returns></returns>
            int GetPageByGridPosition(Point gridPosition)
            {
                return (int)(gridPosition.Y / PageGridHeight);
            }

            int GetPageByPosition(Point worksheetPosition)
            {
                Point gridPosition = viewModel.WorksheetLocationToGridLocation(worksheetPosition);
                return GetPageByGridPosition(gridPosition);
            }

            void Destroy()
            {
                foreach (var item in crosshairInstances)
                {
                    RemoveCrosshairOnPage(item.Value, item.Key);
                }
            }
        }
    }
}

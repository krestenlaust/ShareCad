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

        private const string ConnectionIcon = @"Images\connect_icon.png";

        public readonly EngineeringDocument Document;
        private readonly IWorksheetViewModel viewModel;

        public readonly NetworkClient NetworkClient;
        private readonly Logging.Logger log;
        private readonly Timer networkPushDebounce = new Timer();
        private bool ignoreFirstNetworkPush = true;

        /// <summary>
        /// Creates a SharedDocument instance to keep track on sharing in this document.
        /// </summary>
        /// <param name="engineeringDocument"></param>
        public SharedDocument(EngineeringDocument engineeringDocument, NetworkClient client, Logging.Logger logger)
        {
            log = logger;

            NetworkClient = client;

            Document = engineeringDocument;
            viewModel = engineeringDocument._worksheet.GetViewModel();

            Document.Worksheet.PropertyChanged += PropertyChanged;
            Document.KeyUp += MousePositionMightveChanged;

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

            client.OnWorksheetUpdate += UpdateWorksheet;
            client.OnCollaboratorCursorUpdate += LocalUpdateCursorPosition;

            Document.DocumentTabIcon = System.IO.Path.GetFullPath(ConnectionIcon);
            Document.DocumentName = client.Endpoint.ToString();
        }

        public void StopSharing()
        {
            Document.Worksheet.PropertyChanged -= PropertyChanged;
            Document.KeyUp -= MousePositionMightveChanged;

            Document.DocumentTabIcon = "";
        }

        private void PushDocument()
        {
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

        private void UpdateWorksheet(XmlDocument doc)
        {
            Document.Dispatcher.Invoke(() =>
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
            });
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsCalculating")
            {
                networkPushDebounce.Interval = Update_DebounceTimeout;
                networkPushDebounce.Start();
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

        private Dictionary<byte, CollaboratorCrosshair> collaboratorCrosshairs = new Dictionary<byte, CollaboratorCrosshair>();

        public void LocalUpdateCursorPosition(byte ID, Point position)
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

                //Crosshair crosshair;
                //if (!crosshairs.TryGetValue(ID, out crosshair))
                //{
                //    // instantiate crosshair.
                //    if (!TryInstantiateCrosshair(out crosshair))
                //    {
                //        Console.WriteLine("Crosshair instantiation failed!!!");
                //        return;
                //    }

                //    crosshairs[ID] = crosshair;
                //}

                //MoveCrosshairLegacy(crosshair, position);
            });
        }

        /*
        private void MoveCrosshairLegacy(Crosshair crosshair, Point newPosition)
        {
            Canvas.SetLeft(crosshair, newPosition.X);
            Canvas.SetTop(crosshair, newPosition.Y);
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
            private Dictionary<int, Crosshair> crosshairInstances = new Dictionary<int, Crosshair>();
            private SolidColorBrush crosshairColor = Brushes.Green;
            private int previousPage;
            private IWorksheetViewModel viewModel;

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

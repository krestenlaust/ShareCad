﻿using DevComponents.WpfRibbon;
using ShareCad.Networking;
using Spirit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Ptc.Wpf;
using Ptc.Controls;

namespace ShareCad
{
    public static class ShareCadRibbonParts
    {
        private static ButtonPanel CreateButtonPanel()
        {
            return new ButtonPanel
            {
                MaxHeight = 85,
            };
        }

        // TODO: Crashes when file isn't found.
        private static Image CreateImage(string source)
        {
            try
            {
                return new Image
                {
                    MaxHeight = 32,
                    MaxWidth = 32,
                    Source = new BitmapImage(new Uri(source, UriKind.RelativeOrAbsolute))
                };
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private static ButtonEx CreateButtonEx(string header, Image image, RoutedEventHandler clickEvent)
        {
            ButtonEx button = new ButtonEx
            {
                Header = header,
                ImagePosition = eButtonImagePosition.Top,
                RenderSize = new Size(48, 68),
                Image = image,
                ImageSmall = image
            };

            if (clickEvent is null)
            {
                return button;
            }

            button.Click += clickEvent;
            return button;
        }

        private class LiveShare_GeneralRibbonBar : RibbonBar
        {
            public LiveShare_GeneralRibbonBar()
            {
                Height = 85;
                Header = "General";

                ButtonPanel panel = CreateButtonPanel();
                Items.Add(panel);

                ButtonEx hostNewButton = CreateButtonEx(
                    "Share new document",
                    CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.ShareIconNew)),
                    delegate {
                        ShareCad.NewDocumentAction = NetworkFunction.Host;
                        AppCommands.NewEngineeringDocument.Execute(null, ShareCad.SpiritMainWindow);
                    }
                );
                panel.Children.Add(hostNewButton);

                ButtonEx hostCurrentButton = CreateButtonEx(
                    "Share document",
                    CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.ShareIcon)),
                    delegate
                    {
                        EngineeringDocument currentDocument = ShareCad.GetCurrentTabDocument();

                        if (!(ShareCad.GetSharedDocumentByEngineeringDocument(currentDocument) is null))
                        {
                            Console.WriteLine("Already shared");
                            return;
                        }

                        if (currentDocument is null)
                        {
                            return;
                        }

                        var sharedDoc = ShareCad.StartSharedDocument(currentDocument);
                        ShareCad.NetworkManager.FocusedClient = sharedDoc.NetworkClient;
                    }
                );
                panel.Children.Add(hostCurrentButton);

                ButtonEx guestButton = CreateButtonEx(
                    "Connect to IP/hostname",
                    CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.IpIcon)),
                    delegate {
                        InquireIP result = new InquireIP();

                        var dlgResult = result.Show(WpfUtils.ApplicationFullName);
                        if (dlgResult != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        ShareCad.NewDocumentIP = result.IP;
                        ShareCad.NewDocumentPort = result.Port;
                        ShareCad.NewDocumentAction = NetworkFunction.Guest;
                        AppCommands.NewEngineeringDocument.Execute(null, ShareCad.SpiritMainWindow);
                    }
                );
                panel.Children.Add(guestButton);
            }
        }

        private class LiveShare_CollaboratorManager : RibbonBar
        {
            public LiveShare_CollaboratorManager()
            {
                Height = 85;
                Header = "Collaborators";

                ButtonPanel panel = CreateButtonPanel();
                Items.Add(panel);

                ButtonEx disconnectAllButton = CreateButtonEx(
                    "Stop sharing",
                    null,
                    delegate
                    {
                        ShareCad.NetworkManager.Stop();
                    });
                panel.Children.Add(disconnectAllButton);
            }
        }

        public static void ExtendRibbonControl(Ribbon ribbon)
        {
            RibbonBarPanel ribbonBarPanel = new RibbonBarPanel();
            ribbonBarPanel.Children.Add(new LiveShare_GeneralRibbonBar());
            ribbonBarPanel.Children.Add(new LiveShare_CollaboratorManager());

            RibbonTab ribbonTab = new RibbonTab
            {
                Header = "Live Share",
                Content = ribbonBarPanel
            };

            ribbon.Items.Add(ribbonTab);
        }
    }
}

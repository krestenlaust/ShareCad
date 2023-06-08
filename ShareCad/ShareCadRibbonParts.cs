using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using DevComponents.WpfRibbon;
using ShareCad.Networking;
using Spirit;
using Ptc.Wpf;
using Ptc.Controls;
using System.Windows.Media;

namespace ShareCad
{
    /// <summary>
    /// Keeps track of, and instantiates the GUI elements that are associated with this extension.
    /// TODO: Move UI elements into separate project.
    /// </summary>
    public static class ShareCadRibbonParts
    {
        static ButtonPanel CreateButtonPanel()
        {
            return new ButtonPanel
            {
                MaxHeight = 85,
            };
        }

        // TODO: Crashes when file isn't found.
        static Image CreateImage(string source)
        {
            try
            {
                return new Image
                {
                    MaxHeight = 32,
                    MaxWidth = 32,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Opacity = 0.3f,
                    SnapsToDevicePixels = true,
                    AllowDrop = true,
                    Source = new BitmapImage(new Uri(source, UriKind.RelativeOrAbsolute))
                };
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        static ButtonEx CreateButtonEx(string header, Image image, RoutedEventHandler clickEvent)
        {
            ButtonEx button = new ButtonEx
            {
                Header = header,
                ImagePosition = eButtonImagePosition.Top,
                RenderSize = new Size(48, 68),
                MinHeight = 68,
                Image = image,
                ImageSmall = image,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 141, 141, 141))
            };

            if (clickEvent is null)
            {
                return button;
            }

            button.Click += clickEvent;
            return button;
        }

        class StylisedRibbonBar : RibbonBar
        {
            public StylisedRibbonBar()
            {
                Height = 87;
                Margin = new Thickness(0, -1, 1, -1);
                VerticalAlignment = VerticalAlignment.Stretch;
                HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        class LiveShare_GeneralRibbonBar : StylisedRibbonBar
        {
            public LiveShare_GeneralRibbonBar() : base()
            {
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

        class LiveShare_CollaboratorManager : StylisedRibbonBar
        {
            public LiveShare_CollaboratorManager() : base()
            {
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

using DevComponents.WpfRibbon;
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
            return new Image
            {
                MaxHeight = 32,
                MaxWidth = 32,
                Source = new BitmapImage(new Uri(source, UriKind.RelativeOrAbsolute))
            };
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

                ButtonEx hostButton = CreateButtonEx(
                    "Share document",
                    CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.ShareIcon)),
                    delegate {
                        ShareCad.NewDocumentAction = NetworkFunction.Host;
                        AppCommands.NewEngineeringDocument.Execute(null, ShareCad.SpiritMainWindow);
                    }
                );
                panel.Children.Add(hostButton);

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

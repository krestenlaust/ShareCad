using DevComponents.WpfRibbon;
using ShareCad.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ShareCad
{
    public static class ShareCadRibbonParts
    {
        private class LiveShare_GeneralRibbonBar : RibbonBar
        {
            public LiveShare_GeneralRibbonBar()
            {
                Height = 85;
                Header = "General";

                ButtonPanel panel = new ButtonPanel
                {
                    MaxHeight = 85
                };
                Items.Add(panel);

                Image shareIcon = new Image
                {
                    MaxHeight = 32,
                    MaxWidth = 32,
                    Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(@"Images\share_icon.png"), UriKind.Absolute))
                };
                ButtonEx hostButton = new ButtonEx
                {
                    Header = "Share document",
                    ImagePosition = eButtonImagePosition.Top,
                    RenderSize = new Size(48, 68),
                    Image = shareIcon,
                    ImageSmall = shareIcon
                };
                hostButton.Click += delegate { ShareCad.NewDocumentAction = NetworkFunction.Host; ShareCad.log.Print(ShareCad.NewDocumentAction); };
                panel.Children.Add(hostButton);

                Image ipIcon = new Image
                {
                    MaxHeight = 32,
                    MaxWidth = 32,
                    Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(@"Images\ip_icon.png"), UriKind.Absolute))
                };
                ButtonEx guestButton = new ButtonEx
                {
                    Header = "Connect to ...",
                    ImagePosition = eButtonImagePosition.Top,
                    RenderSize = new Size(48, 68),
                    Image = ipIcon,
                    ImageSmall = ipIcon
                };
                guestButton.Click += delegate { ShareCad.NewDocumentAction = NetworkFunction.Guest; ShareCad.log.Print(ShareCad.NewDocumentAction); };
                panel.Children.Add(guestButton);
            }
        }

        private class LiveShare_CollaboratorManager : RibbonBar
        {
            public LiveShare_CollaboratorManager()
            {
                Height = 85;
                Header = "Collaborators";

                ButtonPanel panel = new ButtonPanel
                {
                    MaxHeight = 85
                };
                Items.Add(panel);

                ButtonEx disconnectAllButton = new ButtonEx
                {
                    Header = "Disconnect all",
                    ImagePosition = eButtonImagePosition.Top,
                    RenderSize = new Size(48, 68),
                    Image = null,
                    ImageSmall = null
                };
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

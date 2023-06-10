using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using DevComponents.WpfRibbon;

namespace ShareCad.UI
{
    /// <summary>
    /// Keeps track of, and instantiates the GUI elements that are associated with this extension.
    /// TODO: Move UI elements into separate project.
    /// </summary>
    public static class ShareCadRibbon
    {
        // Collaborator management ribbonbar
        public static event Action StopSharingPressed;

        // General liveshare ribbonbar
        public static event Action ShareNewDocumentPressed;
        public static event Action ShareCurrentDocumentPressed;
        public static event Action ConnectToDocumentPressed;

        public static ButtonPanel CreateButtonPanel()
        {
            return new ButtonPanel
            {
                MaxHeight = 85,
            };
        }

        // TODO: Crashes when file isn't found.
        public static Image CreateImage(string source)
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

        public static ButtonEx CreateButtonEx(string header, Image image, RoutedEventHandler clickEvent)
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

        public static void ExtendRibbonControl(Ribbon ribbon)
        {
            RibbonBarPanel ribbonBarPanel = new RibbonBarPanel();
            ribbonBarPanel.Children.Add(new GeneralLiveShareRibbonBar(ShareNewDocumentPressed, ShareCurrentDocumentPressed, ConnectToDocumentPressed));
            ribbonBarPanel.Children.Add(new CollaboratorManagementBar(StopSharingPressed));

            RibbonTab ribbonTab = new RibbonTab
            {
                Header = "Live Share",
                Content = ribbonBarPanel
            };

            ribbon.Items.Add(ribbonTab);
        }
    }
}

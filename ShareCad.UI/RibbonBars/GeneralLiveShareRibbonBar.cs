using System;
using System.IO;
using DevComponents.WpfRibbon;
using static ShareCad.UI.ShareCadRibbon;

namespace ShareCad.UI
{
    public class GeneralLiveShareRibbonBar : StylisedRibbonBar
    {
        public GeneralLiveShareRibbonBar(Action shareNewDocumentPressed, Action shareCurrentDocumentPressed, Action connectToDocumentPressed) : base()
        {
            Header = "General";

            ButtonPanel panel = CreateButtonPanel();
            Items.Add(panel);

            ButtonEx hostNewButton = CreateButtonEx(
                "Share new document",
                CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.ShareIconNew)),
                delegate { shareNewDocumentPressed(); }
            );
            panel.Children.Add(hostNewButton);

            ButtonEx hostCurrentButton = CreateButtonEx(
                "Share document",
                CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.ShareIcon)),
                delegate { shareCurrentDocumentPressed(); }
            );
            panel.Children.Add(hostCurrentButton);

            ButtonEx guestButton = CreateButtonEx(
                "Connect to IP/hostname",
                CreateImage(Path.GetFullPath(BootlegResourceManager.Icons.IpIcon)),
                delegate { connectToDocumentPressed(); }
            );
            panel.Children.Add(guestButton);
        }
    }
}

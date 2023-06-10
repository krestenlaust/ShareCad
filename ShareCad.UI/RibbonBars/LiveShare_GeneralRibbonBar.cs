using System;
using System.IO;
using DevComponents.WpfRibbon;
using static ShareCad.UI.ShareCadRibbon;

namespace ShareCad.UI
{
    public class LiveShare_GeneralRibbonBar : StylisedRibbonBar
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
}

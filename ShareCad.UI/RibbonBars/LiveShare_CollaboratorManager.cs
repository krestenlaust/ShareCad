using ShareCad.Networking;
using DevComponents.WpfRibbon;
using static ShareCad.UI.ShareCadRibbon;
//using Spirit;
//using Ptc.Wpf;
//using Ptc.Controls;

namespace ShareCad.UI
{
    public class LiveShare_CollaboratorManager : StylisedRibbonBar
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
}

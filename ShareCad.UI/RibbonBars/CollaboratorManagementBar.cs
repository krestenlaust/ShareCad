using DevComponents.WpfRibbon;
using static ShareCad.UI.ShareCadRibbon;
using System;

namespace ShareCad.UI
{
    public class CollaboratorManagementBar : StylisedRibbonBar
    {
        public CollaboratorManagementBar(Action stopSharingPressed) : base()
        {
            Header = "Collaborators";

            ButtonPanel panel = CreateButtonPanel();
            Items.Add(panel);

            ButtonEx disconnectAllButton = CreateButtonEx(
                "Stop sharing",
                null,
                delegate { stopSharingPressed(); }
            );
            panel.Children.Add(disconnectAllButton);
        }
    }
}

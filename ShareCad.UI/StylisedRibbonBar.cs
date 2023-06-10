using DevComponents.WpfRibbon;
using System.Windows;

namespace ShareCad.UI
{
    public class StylisedRibbonBar : RibbonBar
    {
        public StylisedRibbonBar()
        {
            Height = 87;
            Margin = new Thickness(0, -1, 1, -1);
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Center;
        }
    }
}

using KubeNavigator.ViewModels.Navigation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KubeNavigator.Converters;

public static class NavigationIconHelper
{
    public static IconElement CreateIcon(INavigationGroupIcon icon)
    {
        return icon switch
        {
            PathNavigationGroupIcon path => new PathIcon
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), path.PathData),
            },
            SymbolNavigationGroupIcon symbol => new FontIcon
            {
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                Glyph = symbol.Glyph,
            },
            _ => new FontIcon { Glyph = "\uE74C" },
        };
    }
}

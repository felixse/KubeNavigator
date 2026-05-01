using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is KubernetesResourceViewModel)
        {
            return (DataTemplate)Application.Current.Resources["DynamicTableRowTemplate"];
        }
        else if (item is HelmReleaseViewModel)
        {
            return (DataTemplate)Application.Current.Resources["HelmReleaseTableRow"];
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

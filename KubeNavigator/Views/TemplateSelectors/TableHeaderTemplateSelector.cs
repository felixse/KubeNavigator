using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableHeaderTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is KubernetesResourceTypeListViewModel)
        {
            return (DataTemplate)Application.Current.Resources["DynamicTableHeaderTemplate"];
        }
        else if (item is HelmReleasesViewModel)
        {
            return (DataTemplate)Application.Current.Resources["HelmReleaseTableHeader"];
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

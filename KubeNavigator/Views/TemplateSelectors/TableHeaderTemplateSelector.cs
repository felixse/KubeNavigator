using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ForwardedPortTemplate { get; set; }

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
        else if (item is PortForwardsViewModel && ForwardedPortTemplate != null)
        {
            return ForwardedPortTemplate;
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

using KubeNavigator.Model;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate? KubernetesResourceTemplate { get; set; }

    public DataTemplate? HelmReleaseTemplate { get; set; }

    public DataTemplate? ForwardedPortTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is KubernetesResourceTypeListViewModel kubernetesResourceTypeListViewModel && KubernetesResourceTemplate != null)
        {
            if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Pod)
            {
                var foo = Application.Current.Resources["PodTableHeader"];
                return foo as DataTemplate;
            }
            return KubernetesResourceTemplate;
        }
        else if (item is HelmReleasesViewModel && HelmReleaseTemplate != null)
        {
            return HelmReleaseTemplate;
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

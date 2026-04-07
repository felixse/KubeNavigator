using KubeNavigator.Models;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate? KubernetesResourceTemplate { get; set; }

    public DataTemplate? ForwardedPortTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (
            item is KubernetesResourceTypeListViewModel kubernetesResourceTypeListViewModel
            && KubernetesResourceTemplate != null
        )
        {
            if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Pod)
            {
                var podTableHeader = Application.Current.Resources["PodTableHeader"];
                return (DataTemplate)podTableHeader;
            }
            else if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Service)
            {
                var serviceTableHeader = Application.Current.Resources["ServiceTableHeader"];
                return (DataTemplate)serviceTableHeader;
            }
            else if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.ConfigMap)
            {
                var configMapTableHeader = Application.Current.Resources["ConfigMapTableHeader"];
                return (DataTemplate)configMapTableHeader;
            }
            else if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Secret)
            {
                var secretTableHeader = Application.Current.Resources["SecretTableHeader"];
                return (DataTemplate)secretTableHeader;
            }
            else if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Event)
            {
                var eventTableHeader = Application.Current.Resources["EventTableHeader"];
                return (DataTemplate)eventTableHeader;
            }
            else if (kubernetesResourceTypeListViewModel.ResourceType == ResourceType.Namespace)
            {
                var namespaceTableHeader = Application.Current.Resources["NamespaceTableHeader"];
                return (DataTemplate)namespaceTableHeader;
            }
            return KubernetesResourceTemplate;
        }
        else if (item is HelmReleasesViewModel)
        {
            var helmReleaseTableHeader = Application.Current.Resources["HelmReleaseTableHeader"];
            return (DataTemplate)helmReleaseTableHeader;
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

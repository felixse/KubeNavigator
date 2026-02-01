using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class TableTemplateSelector : DataTemplateSelector
{
    public DataTemplate? KubernetesResourceTemplate { get; set; }

    public DataTemplate? HelmReleaseTemplate { get; set; }

    public DataTemplate? ForwardedPortTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is KubernetesResourceViewModel && KubernetesResourceTemplate != null)
        {
            if (item is PodViewModel)
            {
                var podTableRow = Application.Current.Resources["PodTableRow"];
                return (DataTemplate)podTableRow;
            }
            else if (item is ServiceViewModel)
            {
                var serviceTableRow = Application.Current.Resources["ServiceTableRow"];
                return (DataTemplate)serviceTableRow;
            }
            else if (item is ConfigMapViewModel)
            {
                var configMapTableRow = Application.Current.Resources["ConfigMapTableRow"];
                return (DataTemplate)configMapTableRow;
            }
            else if (item is SecretViewModel)
            {
                var secretTableRow = Application.Current.Resources["SecretTableRow"];
                return (DataTemplate)secretTableRow;
            }
            else if (item is EventViewModel)
            {
                var eventTableRow = Application.Current.Resources["EventTableRow"];
                return (DataTemplate)eventTableRow;
            }
            else if (item is NamespaceViewModel)
            {
                var namespaceTableRow = Application.Current.Resources["NamespaceTableRow"];
                return (DataTemplate)namespaceTableRow;
            }
            return KubernetesResourceTemplate;
        }
        else if (item is HelmReleaseViewModel && HelmReleaseTemplate != null)
        {
            return HelmReleaseTemplate;
        }
        else if (item is ForwardedPortViewModel && ForwardedPortTemplate != null)
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

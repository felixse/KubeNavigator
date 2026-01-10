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

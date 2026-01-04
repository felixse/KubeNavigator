using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class NavigationViewMenuItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CategoryTemplate { get; set; }

    public DataTemplate? ResourceTypeTemplate { get; set; }

    public DataTemplate? PinnedResourceTypeTemplate { get; set; }

    public DataTemplate? SettingsTemplate { get; set; }

    public DataTemplate? PortForwardsTemplate { get; set; }

    public DataTemplate? ClusterListTemplate { get; set; }

    public DataTemplate? CustomResourceGroupTemplate { get; set; }

    public DataTemplate? GenericTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is NavigationGroupViewModel && CategoryTemplate != null)
        {
            return CategoryTemplate;
        }
        else if (item is KubernetesResourceTypeListViewModel && ResourceTypeTemplate != null)
        {
            return ResourceTypeTemplate;
        }
        else if (item is PinnedNavigationTargetViewModel && PinnedResourceTypeTemplate != null)
        {
            return PinnedResourceTypeTemplate;
        }
        else if (item is SettingsViewModel && SettingsTemplate != null)
        {
            return SettingsTemplate;
        }
        else if (item is PortForwardsViewModel && PortForwardsTemplate != null)
        {
            return PortForwardsTemplate;
        }
        else if (item is ClusterListViewModel && ClusterListTemplate != null)
        {
            return ClusterListTemplate;
        }
        else if (item is CustomResourceGroupViewModel && CustomResourceGroupTemplate != null)
        {
            return CustomResourceGroupTemplate;
        }
        else if (item is INavigationTarget && GenericTemplate != null)
        {
            return GenericTemplate;
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

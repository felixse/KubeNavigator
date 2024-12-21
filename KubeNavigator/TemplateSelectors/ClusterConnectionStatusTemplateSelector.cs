using KubeNavigator.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class ClusterConnectionStatusTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DefaultTemplate { get; set; }

    public DataTemplate? ConnectingTemplate { get; set; }

    public DataTemplate? ConnectedTemplate { get; set; }

    public DataTemplate? ErrorTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is ClusterStatus status)
        {
            return status.Status switch
            {
                ConnectionStatus.Connected => ConnectedTemplate!,
                ConnectionStatus.Connecting => ConnectingTemplate!,
                ConnectionStatus.Error => ErrorTemplate!,
                _ => DefaultTemplate!,
            };
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

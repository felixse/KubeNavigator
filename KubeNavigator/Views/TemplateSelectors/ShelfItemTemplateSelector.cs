using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class ShelfItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? EditTemplate { get; set; }

    public DataTemplate? LogsTemplate { get; set; }

    public DataTemplate? ShellTemplate { get; set; }

    public DataTemplate? ApplicationLogTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is EditKubernetesResourceViewModel && EditTemplate != null)
        {
            return EditTemplate;
        }
        else if (item is PodLogsViewModel && LogsTemplate != null)
        {
            return LogsTemplate;
        }
        else if (item is PodShellViewModel && ShellTemplate != null)
        {
            return ShellTemplate;
        }
        else if (item is ApplicationLogViewModel && ApplicationLogTemplate != null)
        {
            return ApplicationLogTemplate;
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

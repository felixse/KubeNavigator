using KubeNavigator.ViewModels.Filters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class NamespaceFilterTemplateSelector : DataTemplateSelector
{
    public DataTemplate? AllNamespacesTemplate { get; set; }

    public DataTemplate? NamespaceTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is AllNamespacesFilter && AllNamespacesTemplate != null)
        {
            return AllNamespacesTemplate;
        }
        else if (item is NamespaceFilter && NamespaceTemplate != null)
        {
            return NamespaceTemplate;
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

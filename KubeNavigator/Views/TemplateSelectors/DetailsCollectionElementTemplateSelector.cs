using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsCollectionElementTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextElementTemplate { get; set; }

    public DataTemplate? ConditionElementTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            ConditionCollectionElement => ConditionElementTemplate,
            TextCollectionElement => TextElementTemplate,
            _ => TextElementTemplate,
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

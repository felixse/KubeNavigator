using KubeNavigator.Model.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? CollectionTemplate { get; set; }

    public DataTemplate? LinkTemplate { get; set; }

    public DataTemplate? PortsTemplate { get; set; }

    public DataTemplate? TableTemplate { get; set; }

    public DataTemplate? DictionaryTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            DetailsLinkItem => LinkTemplate,
            DetailsCollectionItem => CollectionTemplate,
            DetailsPortsItem => PortsTemplate,
            DetailsTableItem => TableTemplate,
            DetailsDictionaryItem => DictionaryTemplate,
            _ => TextTemplate
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

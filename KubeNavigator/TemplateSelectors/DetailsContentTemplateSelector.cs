using KubeNavigator.Model.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? CollectionTemplate { get; set; }

    public DataTemplate? CollectionStackTemplate { get; set; }

    public DataTemplate? LinkTemplate { get; set; }

    public DataTemplate? PortsTemplate { get; set; }

    public DataTemplate? TableTemplate { get; set; }

    public DataTemplate? DictionaryTemplate { get; set; }

    public DataTemplate? MarkdownTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            DetailsLinkItem => LinkTemplate,
            DetailsCollectionItem { IsWrapLayout: true } => CollectionTemplate,
            DetailsCollectionItem => CollectionStackTemplate,
            DetailsPortsItem => PortsTemplate,
            DetailsTableItem => TableTemplate,
            DetailsDictionaryItem => DictionaryTemplate,
            DetailsMarkdownItem => MarkdownTemplate,
            _ => TextTemplate
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

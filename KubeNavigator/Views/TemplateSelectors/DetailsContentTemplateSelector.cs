using KubeNavigator.ViewModels.Details;
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

    public DataTemplate? TableInlineTemplate { get; set; }

    public DataTemplate? DictionaryTemplate { get; set; }

    public DataTemplate? MarkdownTemplate { get; set; }

    public DataTemplate? ConditionsTemplate { get; set; }

    public DataTemplate? EditorTemplate { get; set; }

    public DataTemplate? EditorColumnTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            DetailsLinkItem => LinkTemplate,
            DetailsCollectionItem { IsWrapLayout: true } => CollectionTemplate,
            DetailsCollectionItem => CollectionStackTemplate,
            DetailsPortsItem => PortsTemplate,
            DetailsTableItem { IsExpandable: false } => TableInlineTemplate,
            DetailsTableItem => TableTemplate,
            DetailsDictionaryItem => DictionaryTemplate,
            DetailsMarkdownItem => MarkdownTemplate,
            DetailsConditionsItem => ConditionsTemplate,
            DetailsEditorItem { ShowTitleInColumn: true } => EditorColumnTemplate,
            DetailsEditorItem => EditorTemplate,
            _ => TextTemplate,
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

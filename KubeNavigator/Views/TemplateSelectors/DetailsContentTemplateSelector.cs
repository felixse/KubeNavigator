using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? CollectionWrapTemplate { get; set; }

    public DataTemplate? CollectionStackTemplate { get; set; }

    public DataTemplate? LinkTemplate { get; set; }

    public DataTemplate? PortsTemplate { get; set; }

    public DataTemplate? TableTemplate { get; set; }

    public DataTemplate? DictionaryTemplate { get; set; }

    public DataTemplate? MarkdownTemplate { get; set; }

    public DataTemplate? EditorTemplate { get; set; }

    public DataTemplate? SensitiveDataTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            LinkContent => LinkTemplate,
            CollectionContent { Layout: CollectionLayout.Wrap } => CollectionWrapTemplate,
            CollectionContent => CollectionStackTemplate,
            PortsContent => PortsTemplate,
            TableContent => TableTemplate,
            DictionaryContent => DictionaryTemplate,
            MarkdownContent => MarkdownTemplate,
            EditorContent => EditorTemplate,
            SensitiveDataContent => SensitiveDataTemplate,
            TextContent => TextTemplate,
            _ => TextTemplate,
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

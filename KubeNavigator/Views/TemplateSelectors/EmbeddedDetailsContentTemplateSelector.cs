using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class EmbeddedDetailsContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? EmbeddedTableTemplate { get; set; }

    public DataTemplate? EmbeddedEditorTemplate { get; set; }

    public DetailsContentTemplateSelector? FallbackSelector { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            TableContent => EmbeddedTableTemplate,
            EditorContent => EmbeddedEditorTemplate,
            _ => null,
        };

        return template ?? FallbackSelector?.SelectTemplate(item) ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

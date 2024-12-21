using KubeNavigator.Model.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SectionTemplate { get; set; }

    public DataTemplate? GroupedSectionTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item is GroupedDetailsSection ? GroupedSectionTemplate : SectionTemplate;

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

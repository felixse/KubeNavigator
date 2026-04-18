using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.TemplateSelectors;

public partial class DetailsRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderedRowTemplate { get; set; }

    public DataTemplate? FullWidthRowTemplate { get; set; }

    public DataTemplate? GroupRowTemplate { get; set; }

    public DataTemplate? SegmentedRowTemplate { get; set; }

    public DataTemplate? ExpandableRowTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var template = item switch
        {
            HeaderedRow => HeaderedRowTemplate,
            FullWidthRow => FullWidthRowTemplate,
            GroupRow => GroupRowTemplate,
            SegmentedRow => SegmentedRowTemplate,
            ExpandableRow => ExpandableRowTemplate,
            _ => HeaderedRowTemplate,
        };

        return template ?? base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}

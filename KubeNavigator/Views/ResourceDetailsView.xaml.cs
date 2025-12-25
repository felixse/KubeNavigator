using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using KubeNavigator.Model.Details;
using KubeNavigator.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KubeNavigator.Views;

public sealed partial class ResourceDetailsView : UserControl
{
    private static MarkdownConfig _markdownConfig = new MarkdownConfig
    {
        Themes = new()
        {
            InlineCodePadding = new(4),
            InlineCodeBorderBrush = new SolidColorBrush(Colors.Transparent),
            InlineCodeBorderThickness = new(4),
        }
    };

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(DetailsViewModel), typeof(ResourceDetailsView), new PropertyMetadata(null));

    public DetailsViewModel? ViewModel
    {
        get { return (DetailsViewModel?)GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }

    public static MarkdownConfig GetMarkdownConfig()
    {
        return _markdownConfig;
    }

    public ResourceDetailsView()
    {
        this.InitializeComponent();
    }

    private void HyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is HyperlinkButton hyperlink && hyperlink.DataContext is DetailsLinkItem linkItem && ViewModel != null)
        {
            ViewModel.NavigateAsync(linkItem);
        }
    }
}

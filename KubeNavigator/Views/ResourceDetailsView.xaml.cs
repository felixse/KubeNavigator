using CommunityToolkit.WinUI.Controls;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Details;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using WinUIEditor;

namespace KubeNavigator.Views;

public sealed partial class ResourceDetailsView : UserControl
{
    private static MarkdownConfig _markdownConfig = new MarkdownConfig
    {
        //Themes = new()
        //{
        //    InlineCodePadding = new(4),
        //    InlineCodeBorderBrush = new SolidColorBrush(Colors.Transparent),
        //    InlineCodeBorderThickness = new(4),
        //},
    };

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DetailsViewModel),
        typeof(ResourceDetailsView),
        new PropertyMetadata(null, OnViewModelChanged)
    );

    public DetailsViewModel? ViewModel
    {
        get { return (DetailsViewModel?)GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResourceDetailsView view)
        {
            view.DetailScrollViewer.ChangeView(null, 0, null, disableAnimation: true);
        }
    }

    public static MarkdownConfig GetMarkdownConfig()
    {
        return _markdownConfig;
    }

    public ResourceDetailsView()
    {
        this.InitializeComponent();
    }

    private async void HyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (
            sender is HyperlinkButton hyperlink
            && hyperlink.DataContext is DetailsLinkItem linkItem
            && ViewModel != null
        )
        {
            await ViewModel.NavigateAsync(linkItem);
        }
    }

    private void CodeEditorControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (
            sender is CodeEditorControl codeEditor
            && codeEditor.DataContext is DetailsEditorItem editorItem
        )
        {
            codeEditor.Editor.SetText(editorItem.Value);
            editorItem.TextRetriever = () => codeEditor.Editor.GetText(long.MaxValue);
        }
    }
}

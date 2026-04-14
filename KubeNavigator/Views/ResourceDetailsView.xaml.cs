using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEditor;

namespace KubeNavigator.Views;

public sealed partial class ResourceDetailsView : UserControl
{
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

    public ResourceDetailsView()
    {
        this.InitializeComponent();
    }

    private async void HyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (
            sender is HyperlinkButton hyperlink
            && hyperlink.DataContext is LinkContent linkContent
            && ViewModel != null
        )
        {
            await ViewModel.NavigateAsync(linkContent);
        }
    }

    private void CodeEditorControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (
            sender is CodeEditorControl codeEditor
            && codeEditor.DataContext is EditorContent editorContent
        )
        {
            codeEditor.Editor.SetText(editorContent.Value);
            editorContent.TextRetriever = () => codeEditor.Editor.GetText(long.MaxValue);
        }
    }
}

using System;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Details;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        var view = (ResourceDetailsView)d;

        if (e.OldValue is DetailsViewModel oldVm)
        {
            oldVm.Navigating -= view.OnNavigating;
            oldVm.NavigatedBack -= view.OnNavigatedBack;
        }

        if (e.NewValue is DetailsViewModel newVm)
        {
            newVm.Navigating += view.OnNavigating;
            newVm.NavigatedBack += view.OnNavigatedBack;
        }

        view.DetailScrollViewer.ChangeView(null, 0, null, disableAnimation: true);
    }

    public ResourceDetailsView()
    {
        this.InitializeComponent();
        this.PointerPressed += OnPointerPressed;
    }

    private void OnNavigating(object? sender, EventArgs e)
    {
        if (ViewModel?.NavigationStack.Count > 0)
        {
            ViewModel.NavigationStack.Peek().ScrollOffset = DetailScrollViewer.VerticalOffset;
        }
    }

    private void OnNavigatedBack(object? sender, NavigationEntry entry)
    {
        var target = entry.ScrollOffset;

        // Try immediately — works if content is already tall enough
        DetailScrollViewer.ChangeView(null, target, null, disableAnimation: true);

        if (DetailScrollViewer.ScrollableHeight >= target)
        {
            return;
        }

        // Content isn't tall enough yet — keep retrying as layout progresses
        DetailScrollViewer.LayoutUpdated += OnLayoutUpdated;

        void OnLayoutUpdated(object? s, object e)
        {
            DetailScrollViewer.ChangeView(null, target, null, disableAnimation: true);

            if (
                DetailScrollViewer.ScrollableHeight >= target
                || DetailScrollViewer.VerticalOffset >= target - 1
            )
            {
                DetailScrollViewer.LayoutUpdated -= OnLayoutUpdated;
            }
        }
    }

    private async void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsXButton1Pressed && ViewModel?.CanGoBack == true)
        {
            e.Handled = true;
            await ViewModel.GoBackAsync();
        }
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

    private async void SimpleTable_LinkClicked(object sender, LinkContent e)
    {
        if (ViewModel != null)
        {
            await ViewModel.NavigateAsync(e);
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

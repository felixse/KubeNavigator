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
        if (sender is not HyperlinkButton hyperlink || ViewModel is null)
        {
            return;
        }

        LinkContent? linkContent = hyperlink.DataContext switch
        {
            LinkContent lc => lc,
            LinkCollectionElement lce => new LinkContent
            {
                ResourceName = lce.ResourceName,
                ResourceType = lce.ResourceType,
            },
            _ => null,
        };

        if (linkContent is not null)
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
            codeEditor.Editor.ReadOnly = editorContent.IsReadOnly;
            codeEditor.Editor.EndAtLastLine = true;
            editorContent.TextRetriever = () => codeEditor.Editor.GetText(long.MaxValue);

            UpdateEditorHeight(codeEditor);

            codeEditor.Editor.UpdateUI += (s, args) => UpdateEditorHeight(codeEditor);
        }
    }

    private static void UpdateEditorHeight(CodeEditorControl codeEditor)
    {
        var scaleFactor = codeEditor.XamlRoot.RasterizationScale;
        var lineHeight = codeEditor.Editor.TextHeight(0) / scaleFactor;
        var lineCount = codeEditor.Editor.LineCount;

        // Scintilla adds a trailing empty line; exclude it from height calculation
        if (lineCount > 1 && codeEditor.Editor.LineLength(lineCount - 1) == 0)
        {
            lineCount--;
        }

        var contentHeight = lineHeight * lineCount;
        var desiredHeight = contentHeight + 36;
        var newHeight = codeEditor.MaxHeight > 0
            ? Math.Min(desiredHeight, codeEditor.MaxHeight)
            : desiredHeight;
        codeEditor.Height = newHeight;

        // If all content fits, scroll to top so no lines are hidden
        if (desiredHeight <= newHeight)
        {
            codeEditor.Editor.FirstVisibleLine = 0;
        }
    }

    private void CodeEditorControl_PointerWheelChanged(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
    )
    {
        if (sender is CodeEditorControl codeEditor)
        {
            var delta = e.GetCurrentPoint(codeEditor).Properties.MouseWheelDelta;
            var firstVisible = codeEditor.Editor.FirstVisibleLine;
            var linesOnScreen = codeEditor.Editor.LinesOnScreen;
            var totalLines = codeEditor.Editor.LineCount;

            var canScrollUp = delta > 0 && firstVisible > 0;
            var canScrollDown = delta < 0 && firstVisible + linesOnScreen < totalLines;

            e.Handled = canScrollUp || canScrollDown;
        }
    }
}

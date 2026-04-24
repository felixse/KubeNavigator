using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;


namespace KubeNavigator.Views;

public sealed partial class ResourceEditView : UserControl, IShelfItemView
{
    public ResourceEditView(EditKubernetesResourceViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.TextRetriever = () => Editor.Editor.GetText(long.MaxValue);
        this.InitializeComponent();

        KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Modifiers = VirtualKeyModifiers.Control,
            Key = VirtualKey.F,
        });

        Task.Run(LoadContentAsync);
    }

    public EditKubernetesResourceViewModel ViewModel { get; }

    protected override void OnKeyboardAcceleratorInvoked(KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.KeyboardAccelerator.Key == VirtualKey.F &&
            args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Control)
        {
            ShowSearch();
            args.Handled = true;
            return;
        }

        base.OnKeyboardAcceleratorInvoked(args);
    }

    private void ShowSearch()
    {
        SearchOverlay.Visibility = Visibility.Visible;
        SearchBox.Focus(FocusState.Programmatic);
        SearchBox.SelectAll();
    }

    private void HideSearch()
    {
        SearchOverlay.Visibility = Visibility.Collapsed;
        Editor.Focus(FocusState.Programmatic);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        var editor = Editor.Editor;
        editor.SetTargetRange(0, editor.TextLength);
        editor.SearchFlags = 0;
        var pos = editor.SearchInTarget(query.Length, query);
        if (pos >= 0)
        {
            editor.SetSel(pos, pos + query.Length);
        }
    }

    private void SearchNext_Click(object sender, RoutedEventArgs e) => FindNext();

    private void SearchPrev_Click(object sender, RoutedEventArgs e) => FindPrev();

    private void CloseSearch_Click(object sender, RoutedEventArgs e) => HideSearch();

    private void SearchNextAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        FindNext();
        args.Handled = true;
    }

    private void SearchPrevAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        FindPrev();
        args.Handled = true;
    }

    private void CloseSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        HideSearch();
        args.Handled = true;
    }

    private void FindNext()
    {
        var query = SearchBox.Text;
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        var editor = Editor.Editor;
        var start = editor.CurrentPos;
        editor.SetTargetRange(start, editor.TextLength);
        editor.SearchFlags = 0;
        var pos = editor.SearchInTarget(query.Length, query);
        if (pos < 0)
        {
            editor.SetTargetRange(0, start);
            pos = editor.SearchInTarget(query.Length, query);
        }

        if (pos >= 0)
        {
            editor.SetSel(pos, pos + query.Length);
        }
    }

    private void FindPrev()
    {
        var query = SearchBox.Text;
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        var editor = Editor.Editor;
        var selStart = Math.Min(editor.CurrentPos, editor.Anchor);
        editor.SetTargetRange(selStart, 0);
        editor.SearchFlags = 0;
        var pos = editor.SearchInTarget(query.Length, query);
        if (pos < 0)
        {
            editor.SetTargetRange(editor.TextLength, selStart);
            pos = editor.SearchInTarget(query.Length, query);
        }

        if (pos >= 0)
        {
            editor.SetSel(pos, pos + query.Length);
        }
    }

    public async Task LoadContentAsync()
    {
        try
        {
            var content = await ViewModel.LoadResourceBodyAsync();

            await DispatcherQueue.EnqueueAsync(() =>
            {
                Editor.Editor.SetText(content);
                ProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                Editor.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            });
        }
        catch (System.Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                ViewModel.Resource.Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Error",
                    $"Failed to load {ViewModel.Resource.Resource.Kind} {ViewModel.Resource.Name}: {ex.Message}",
                    NotificationSeverity.Error
                );

                await ViewModel.Resource.Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(ViewModel);
            });
        }
    }
}

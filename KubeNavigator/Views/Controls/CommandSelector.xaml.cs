using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

namespace KubeNavigator.Views.Controls;

public sealed partial class CommandSelector : UserControl
{
    public WindowViewModel ViewModel
    {
        get { return (WindowViewModel)GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(WindowViewModel),
        typeof(CommandSelector),
        new PropertyMetadata(null)
    );

    public CommandSelector()
    {
        InitializeComponent();
    }

    private void Query_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.SelectedWorkspace.ExecuteSelectedCommand();
        }
        else if (e.Key == VirtualKey.Up)
        {
            ViewModel.SelectedWorkspace.SelectPreviousCommand();
        }
        else if (e.Key == VirtualKey.Down)
        {
            ViewModel.SelectedWorkspace.SelectNextCommand();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            ViewModel.IsCommandPanelOpen = false;
        }
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // todo: this crashes
        //DispatcherQueue.TryEnqueue(() =>
        //{
        //    if (e.AddedItems.Count > 0)
        //    {
        //        Commands.ScrollIntoView(e.AddedItems.First());
        //    }
        //});
    }

    private void SelectorPopup_Opened(object sender, object e)
    {
        Query.Focus(FocusState.Programmatic);
    }

    private async void OnCommandDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await ViewModel.SelectedWorkspace.ExecuteSelectedCommand();
    }
}

using KubeNavigator.ViewModels;
using KubeNavigator.Views;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace KubeNavigator.Converters;

public partial class WorkspaceViewModelToViewConverter : IValueConverter
{
    private readonly Dictionary<WorkspaceViewModel, WorkspaceView> views = [];

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is WorkspaceViewModel viewModel)
        {
            if (!views.TryGetValue(viewModel, out WorkspaceView? view))
            {
                view = new WorkspaceView(viewModel);
                views.Add(viewModel, view);
                viewModel.Closed += OnClosed;
            }

            return view;
        }

        return null!;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (sender is WorkspaceViewModel viewModel)
        {
            if (views.TryGetValue(viewModel, out WorkspaceView? view))
            {
                views.Remove(viewModel);
                viewModel.Closed -= OnClosed;
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

using KubeNavigator.ViewModels.Shelf;
using KubeNavigator.Views;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace KubeNavigator.Converters;

public partial class ShelfItemToViewConverter : IValueConverter
{
    readonly Dictionary<IShelfItem, IShelfItemView> views = [];

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IShelfItem viewModel)
        {
            if (!views.TryGetValue(viewModel, out var view))
            {
                viewModel.Closed += OnItemClosed;
                view = viewModel switch
                {
                    PodLogsViewModel logViewModel => new PodLogView(logViewModel),
                    EditKubernetesResourceViewModel editViewModel => new ResourceEditView(editViewModel),
                    PodShellViewModel shellViewModel => new PodShellView(shellViewModel),
                    ApplicationLogViewModel appLogViewModel => new ApplicationLogView(appLogViewModel),
                    _ => throw new NotImplementedException()
                };

                views.Add(viewModel, view);
            }

            return view;
        }


        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private void OnItemClosed(object? sender, EventArgs e)
    {
        if (sender is IShelfItem viewModel)
        {
            if (views.TryGetValue(viewModel, out var view))
            {
                views.Remove(viewModel);
                viewModel.Closed -= OnItemClosed;
            }
        }
    }
}

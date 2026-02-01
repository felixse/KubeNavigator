using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace KubeNavigator.Converters;

public partial class NavigationTargetToViewConverter : IValueConverter
{
    private readonly Dictionary<INavigationTarget, UserControl> _views = [];
    private INavigationTarget? _current;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is KubernetesResourceTypeListViewModel resourceTypeList)
        {
            if (!_views.TryGetValue(resourceTypeList, out UserControl? view))
            {
                Task.Run(async () => await resourceTypeList.ActivateAsync());
                view = new ItemListView { ViewModel = resourceTypeList };
                _views.Add(resourceTypeList, view);
            }

            _current?.OnNavigatedFrom();
            _current = resourceTypeList;
            resourceTypeList.OnNavigatedTo();
            return view;
        }
        else if (value is PinnedNavigationTargetViewModel pinnedResourceViewModel)
        {
            return Convert(
                pinnedResourceViewModel.NavigationTarget,
                targetType,
                parameter,
                language
            );
        }
        else if (value is HelmReleasesViewModel helmReleases)
        {
            if (!_views.TryGetValue(helmReleases, out UserControl? view))
            {
                Task.Run(async () => await helmReleases.ActivateAsync());
                view = new ItemListView { ViewModel = helmReleases };
                _views.Add(helmReleases, view);
            }

            _current?.OnNavigatedFrom();
            _current = helmReleases;
            helmReleases.OnNavigatedTo();
            return view;
        }
        else if (value is SettingsViewModel settingsViewModel)
        {
            if (!_views.TryGetValue(settingsViewModel, out UserControl? view))
            {
                view = new SettingsView { ViewModel = settingsViewModel };
                _views.Add(settingsViewModel, view);
            }

            _current?.OnNavigatedFrom();
            _current = settingsViewModel;
            settingsViewModel.OnNavigatedTo();
            return view;
        }
        else if (value is ClusterListViewModel clusterList)
        {
            if (!_views.TryGetValue(clusterList, out UserControl? view))
            {
                view = new ClusterListView { ViewModel = clusterList };
                _views.Add(clusterList, view);
            }

            _current?.OnNavigatedFrom();
            _current = clusterList;
            clusterList.OnNavigatedTo();
            return view;
        }
        else if (value is PortForwardsViewModel portForwards)
        {
            if (!_views.TryGetValue(portForwards, out UserControl? view))
            {
                view = new PortForwardsView { ViewModel = portForwards };
                _views.Add(portForwards, view);
            }

            _current?.OnNavigatedFrom();
            _current = portForwards;
            portForwards.OnNavigatedTo();
            return view;
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

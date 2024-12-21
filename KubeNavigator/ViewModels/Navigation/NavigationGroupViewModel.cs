using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KubeNavigator.ViewModels.Navigation;

public partial class NavigationGroupViewModel : ObservableObject
{
    public NavigationGroupViewModel(string name, string icon, IEnumerable<INavigationTarget> resources)
    {
        Name = name;
        Icon = icon;
        Items = [.. resources];

        Items.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsVisible));
    }

    public string Name { get; }

    public string Icon { get; }

    public bool HideIfEmpty { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public ObservableCollection<INavigationTarget> Items { get; }

    public bool IsVisible => Items.Any();
}

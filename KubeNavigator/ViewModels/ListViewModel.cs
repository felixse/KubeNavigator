using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using KubeNavigator.ViewModels.Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;
public abstract partial class ListViewModel : ObservableObject, INavigationTarget
{
    public string Title { get; }

    [ObservableProperty]
    public partial AdvancedCollectionView? Items { get; protected set; }

    [ObservableProperty]
    public partial ISelectable? SelectedItem { get; set; }

    protected bool _selectingAll;

    public bool IsAllSelected
    {
        get => Items != null && Items.Count != 0 && Items.Cast<ISelectable>().All(r => r.IsSelected);
        set
        {
            _selectingAll = true;
            if (value)
            {
                foreach (var item in Items?.Cast<ISelectable>() ?? [])
                {
                    item.IsSelected = true;
                }
            }
            else if (Items != null && Items.Cast<ISelectable>().All(r => r.IsSelected))
            {
                foreach (var item in Items.Cast<ISelectable>())
                {
                    item.IsSelected = false;
                }
            }

            OnPropertyChanged(nameof(IsAllSelected));
            _selectingAll = false;
        }
    }

    [ObservableProperty]
    public partial string? SearchText { get; set; }
    public WorkspaceViewModel Workspace { get; }
    public bool IsNamespaceScoped { get; }

    public ObservableCollection<INamespaceFilter> NamespaceFilters { get; } // todo should we just pass in generic filters (values + selected) or would this be overengineering?

    public ListViewModel(WorkspaceViewModel workspace, string title, bool isNamespaceScoped, ObservableCollection<INamespaceFilter> namespaceFilters)
    {
        Workspace = workspace;
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Title = title;
        IsNamespaceScoped = isNamespaceScoped;
        NamespaceFilters = namespaceFilters;
    }

    private void OnWorkspacePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.SelectedNamespaceFilter))
        {
            Items?.RefreshFilter();
        }
    }

    public bool CanDeleteSelectedItems()
    {
        return Items != null && Items.Cast<ISelectable>().Any(r => r.IsSelected);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedItems))]
    public async Task DeleteSelectedItemsAsync()
    {
        await DeleteItemsAsync([.. Items.Cast<ISelectable>().Where(r => r.IsSelected)]);
    }

    [RelayCommand]
    public async Task OpenInNewTab()
    {
        await Workspace.Window.OpenInNewWorkspaceAsync(this, Workspace.Cluster);
    }

    abstract protected Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items);

    partial void OnSelectedItemChanged(ISelectable? oldValue, ISelectable? newValue)
    {
        if (newValue != null)
        {
            Workspace.OpenDetails(newValue, this);
        }
        else
        {
            Workspace.ClosePanel();
        }
    }

    partial void OnSearchTextChanged(string? oldValue, string? newValue)
    {
        Items?.RefreshFilter();
    }

    public abstract Task OnNavigatedTo();
    public abstract Task OnNavigatedFrom();
}

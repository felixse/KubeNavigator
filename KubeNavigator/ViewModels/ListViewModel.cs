using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using KubeNavigator.ViewModels.Filters;
using KubeNavigator.ViewModels.Navigation;

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
        get =>
            Items != null && Items.Count != 0 && AllSelected();
        set
        {
            _selectingAll = true;
            if (value)
            {
                foreach (ISelectable item in Items ?? [])
                {
                    item.IsSelected = true;
                }
            }
            else if (Items != null && AllSelected())
            {
                foreach (ISelectable item in Items)
                {
                    item.IsSelected = false;
                }
            }

            OnPropertyChanged(nameof(IsAllSelected));
            _selectingAll = false;
        }
    }

    private bool AllSelected()
    {
        foreach (ISelectable item in Items!)
        {
            if (!item.IsSelected) return false;
        }
        return true;
    }

    [ObservableProperty]
    public partial string? SearchText { get; set; }
    public WorkspaceViewModel Workspace { get; }
    public bool IsNamespaceScoped { get; }

    public ObservableCollection<INamespaceFilter> NamespaceFilters { get; }

    public IEnumerable<ToggleFilter> AdditionalFilters { get; }

    public ListViewModel(
        WorkspaceViewModel workspace,
        string title,
        bool isNamespaceScoped,
        ObservableCollection<INamespaceFilter> namespaceFilters,
        IEnumerable<ToggleFilter> additionalFilters
    )
    {
        Workspace = workspace;
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Title = title;
        IsNamespaceScoped = isNamespaceScoped;
        NamespaceFilters = namespaceFilters;
        AdditionalFilters = additionalFilters;

        foreach (var filter in AdditionalFilters)
        {
            filter.PropertyChanged += OnFilterPropertyChanged;
        }
    }

    private void OnWorkspacePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.SelectedNamespaceFilter))
        {
            Items?.RefreshFilter();
        }
    }

    private void OnFilterPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(ToggleFilter.IsChecked))
        {
            Items?.RefreshFilter();
        }
    }

    public bool CanDeleteSelectedItems()
    {
        if (Items == null) return false;
        foreach (ISelectable item in Items)
        {
            if (item.IsSelected) return true;
        }
        return false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedItems))]
    public async Task DeleteSelectedItemsAsync()
    {
        var selected = new List<ISelectable>();
        foreach (ISelectable item in Items!)
        {
            if (item.IsSelected) selected.Add(item);
        }
        await DeleteItemsAsync(selected);
    }

    [RelayCommand]
    public virtual void AddNewItem() { }

    [RelayCommand]
    public async Task OpenInNewTab()
    {
        await Workspace.Window.OpenInNewWorkspaceAsync(this, Workspace.Cluster);
    }

    protected abstract Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items);

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

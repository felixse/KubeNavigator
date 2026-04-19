using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KubeNavigator.ViewModels.Filters;

public partial class ToggleFilter : ObservableObject
{
    public string Title { get; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    public Predicate<object> Expression { get; }

    public ToggleFilter(string title, bool defaultValue, Predicate<object> expression)
    {
        Title = title;
        IsChecked = defaultValue;
        Expression = expression;
    }
}

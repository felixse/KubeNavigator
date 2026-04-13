using System;

namespace KubeNavigator.ViewModels.Resources;

public enum ResourceColumnType
{
    Text,
    Age,
    Status,
    ContainerStatuses,
    Conditions,
}

/// <param name="Header">The column header text.</param>
/// <param name="ValueAccessor">A delegate that extracts the cell value from a view model.</param>
/// <param name="ColumnType">Controls which control type is used for the cell.</param>
/// <param name="PropertyName">
/// Optional property name on the view model. When supplied the dynamic table
/// path creates a <see cref="Microsoft.UI.Xaml.Data.Binding"/> so the cell
/// updates automatically when the property raises <c>PropertyChanged</c>.
/// </param>
public record ResourceColumn(
    string Header,
    Func<KubernetesResourceViewModel, object?> ValueAccessor,
    ResourceColumnType ColumnType = ResourceColumnType.Text,
    string? PropertyName = null
);

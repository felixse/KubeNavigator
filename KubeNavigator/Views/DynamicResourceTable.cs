using System;
using System.Collections.Immutable;
using CommunityToolkit.WinUI.Controls;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.Views.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace KubeNavigator.Views;

internal static class DynamicResourceTable
{
    // Improvement 3: shared brush — avoids a per-row allocation.
    private static readonly SolidColorBrush TransparentBrush =
        new(Microsoft.UI.Colors.Transparent);

    private static DataTemplate? _containerStatusTemplate;

    private static DataTemplate ContainerStatusTemplate =>
        _containerStatusTemplate ??=
            (DataTemplate)Application.Current.Resources["ContainerStatusItemTemplate"];

    private static DataTemplate? _conditionStatusTemplate;

    private static DataTemplate ConditionStatusTemplate =>
        _conditionStatusTemplate ??=
            (DataTemplate)Application.Current.Resources["ConditionStatusItemTemplate"];

    public static DataTable BuildHeader(
        KubernetesResourceTypeListViewModel listViewModel,
        ImmutableArray<ResourceColumn> columns
    )
    {
        var table = new DataTable { ColumnSpacing = 16 };

        var checkboxColumn = new DataColumn { DesiredWidth = GridLength.Auto };
        checkboxColumn.Margin = new Thickness(12, 0, 0, 0);
        var checkBox = new CheckBox { MinWidth = 36 };
        checkBox.SetBinding(
            Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding
            {
                Source = listViewModel,
                Path = new PropertyPath(nameof(ListViewModel.IsAllSelected)),
                Mode = BindingMode.TwoWay,
            }
        );
        checkboxColumn.Content = checkBox;
        table.Children.Add(checkboxColumn);

        foreach (var column in columns)
        {
            table.Children.Add(
                new DataColumn
                {
                    Content = column.Header,
                    DesiredWidth = GridLength.Auto,
                    CanResize = true,
                    FontWeight = FontWeights.SemiBold,
                }
            );
        }

        return table;
    }

    /// <summary>
    /// Creates a new <see cref="DataRow"/> with the correct number of child
    /// elements for the given column definitions.  Cell values are <b>not</b>
    /// populated — call <see cref="UpdateRow"/> afterwards.
    /// </summary>
    public static DataRow CreateRow(ImmutableArray<ResourceColumn> columns)
    {
        var row = new DataRow
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = TransparentBrush, // Improvement 3
        };

        // Improvement 4: defer context flyout — created lazily on right-click.
        row.ContextRequested += OnRowContextRequested;

        // Checkbox placeholder — binding is set in UpdateRow.
        row.Children.Add(new CheckBox { MinWidth = 36 });

        foreach (var column in columns)
        {
            row.Children.Add(CreateCellElement(column));
        }

        return row;
    }

    /// <summary>
    /// Re-binds an existing <see cref="DataRow"/> (created by <see cref="CreateRow"/>)
    /// to a new view model.  Child elements are reused — only their bindings /
    /// values are updated.  (Improvements 1 &amp; 2)
    /// </summary>
    public static void UpdateRow(
        DataRow row,
        KubernetesResourceViewModel resourceViewModel,
        ImmutableArray<ResourceColumn> columns
    )
    {
        // Store the VM on the row's Tag so the deferred context flyout can
        // find it later (Improvement 4).
        row.Tag = resourceViewModel;

        // Checkbox — index 0
        if (row.Children[0] is CheckBox checkBox)
        {
            checkBox.SetBinding(
                Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding
                {
                    Source = resourceViewModel,
                    Path = new PropertyPath(nameof(KubernetesResourceViewModel.IsSelected)),
                    Mode = BindingMode.TwoWay,
                }
            );
        }

        // Data cells — index 1..N  (Improvement 1: reuse elements, Improvement 2: use Binding)
        for (int i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            var element = row.Children[i + 1]; // +1 for checkbox

            BindCellElement(element, column, resourceViewModel);
        }
    }

    // -- Improvement 4: lazily build the context flyout on first right-click --

    private static void OnRowContextRequested(
        UIElement sender,
        Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args
    )
    {
        if (sender is not DataRow row || row.Tag is not KubernetesResourceViewModel vm)
        {
            return;
        }

        if (
            Application.Current.Resources["ListItemToMenuConverter"]
            is not Converters.ListItemToMenuConverter converter
        )
        {
            return;
        }

        var flyout = converter.Convert(
            vm,
            typeof(Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase),
            null,
            null
        ) as Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase;

        row.ContextFlyout = flyout;

        if (flyout is not null)
        {
            // The framework already evaluated ContextFlyout (it was null) before
            // this handler ran, so it won't show the flyout automatically on the
            // first right-click.  Show it explicitly at the pointer position.
            if (args.TryGetPosition(row, out var point))
            {
                flyout.ShowAt(
                    row,
                    new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
                    {
                        Position = point,
                    }
                );
            }
            else
            {
                flyout.ShowAt(row);
            }

            args.Handled = true;
        }
    }

    // -- Cell creation (structure only, no data) --

    private static UIElement CreateCellElement(ResourceColumn column)
    {
        return column.ColumnType switch
        {
            ResourceColumnType.Age => new AgeTextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
            },
            ResourceColumnType.Status => new StatusTextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
            },
            ResourceColumnType.ContainerStatuses => CreateContainerStatusRepeater(),
            ResourceColumnType.Conditions => CreateConditionStatusRepeater(),
            _ => new TextBlock { VerticalAlignment = VerticalAlignment.Center },
        };
    }

    private static ItemsRepeater CreateContainerStatusRepeater()
    {
        var repeater = new ItemsRepeater
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Layout = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 4 },
            ItemTemplate = ContainerStatusTemplate,
        };
        repeater.ElementPrepared += static (sender, args) =>
        {
            if (args.Element is ContainerStatusView view
                && sender is ItemsRepeater r
                && args.Index < r.ItemsSourceView.Count)
            {
                view.Status = r.ItemsSourceView.GetAt(args.Index) as k8s.Models.V1ContainerStatus;
            }
        };
        repeater.ElementClearing += static (_, args) =>
        {
            if (args.Element is ContainerStatusView view)
            {
                view.Status = null;
            }
        };
        return repeater;
    }

    private static ItemsRepeater CreateConditionStatusRepeater()
    {
        var repeater = new ItemsRepeater
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Layout = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8 },
            ItemTemplate = ConditionStatusTemplate,
        };
        repeater.ElementPrepared += static (sender, args) =>
        {
            if (args.Element is StatusTextBlock statusBlock
                && sender is ItemsRepeater r
                && args.Index < r.ItemsSourceView.Count)
            {
                statusBlock.Status = r.ItemsSourceView.GetAt(args.Index)?.ToString();
            }
        };
        repeater.ElementClearing += static (_, args) =>
        {
            if (args.Element is StatusTextBlock statusBlock)
            {
                statusBlock.Status = null;
            }
        };
        return repeater;
    }

    // -- Improvement 2: bind cell to VM property for live updates --

    private static void BindCellElement(
        UIElement element,
        ResourceColumn column,
        KubernetesResourceViewModel vm
    )
    {
        switch (column.ColumnType)
        {
            case ResourceColumnType.Age when element is AgeTextBlock age:
                if (column.PropertyName is not null)
                {
                    age.SetBinding(
                        AgeTextBlock.TimestampProperty,
                        new Binding
                        {
                            Source = vm,
                            Path = new PropertyPath(column.PropertyName),
                            Mode = BindingMode.OneWay,
                        }
                    );
                }
                else
                {
                    age.Timestamp = column.ValueAccessor(vm) as DateTime?;
                }
                break;

            case ResourceColumnType.Status when element is StatusTextBlock status:
                if (column.PropertyName is not null)
                {
                    status.SetBinding(
                        StatusTextBlock.StatusProperty,
                        new Binding
                        {
                            Source = vm,
                            Path = new PropertyPath(column.PropertyName),
                            Mode = BindingMode.OneWay,
                        }
                    );
                }
                else
                {
                    status.Status = column.ValueAccessor(vm)?.ToString() ?? string.Empty;
                }
                break;

            case ResourceColumnType.ContainerStatuses when element is ItemsRepeater repeater:
                if (column.PropertyName is not null)
                {
                    repeater.SetBinding(
                        ItemsRepeater.ItemsSourceProperty,
                        new Binding
                        {
                            Source = vm,
                            Path = new PropertyPath(column.PropertyName),
                            Mode = BindingMode.OneWay,
                        }
                    );
                }
                else
                {
                    repeater.ItemsSource =
                        column.ValueAccessor(vm) as System.Collections.IEnumerable;
                }
                break;

            case ResourceColumnType.Conditions when element is ItemsRepeater condRepeater:
                if (column.PropertyName is not null)
                {
                    condRepeater.SetBinding(
                        ItemsRepeater.ItemsSourceProperty,
                        new Binding
                        {
                            Source = vm,
                            Path = new PropertyPath(column.PropertyName),
                            Mode = BindingMode.OneWay,
                        }
                    );
                }
                else
                {
                    condRepeater.ItemsSource =
                        column.ValueAccessor(vm) as System.Collections.IEnumerable;
                }
                break;

            default:
                if (element is TextBlock textBlock)
                {
                    if (column.PropertyName is not null)
                    {
                        textBlock.SetBinding(
                            TextBlock.TextProperty,
                            new Binding
                            {
                                Source = vm,
                                Path = new PropertyPath(column.PropertyName),
                                Mode = BindingMode.OneWay,
                            }
                        );
                    }
                    else
                    {
                        textBlock.Text =
                            column.ValueAccessor(vm)?.ToString() ?? string.Empty;
                    }
                }
                break;
        }
    }
}

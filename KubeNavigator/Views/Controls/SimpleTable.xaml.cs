using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views.Controls;

public sealed partial class SimpleTable : UserControl
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns),
        typeof(IEnumerable<string>),
        typeof(SimpleTable),
        new PropertyMetadata(null, OnDataChanged)
    );

    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
        nameof(Rows),
        typeof(IEnumerable<IEnumerable<string>>),
        typeof(SimpleTable),
        new PropertyMetadata(null, OnDataChanged)
    );

    public IEnumerable<string>? Columns
    {
        get => (IEnumerable<string>?)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public IEnumerable<IEnumerable<string>>? Rows
    {
        get => (IEnumerable<IEnumerable<string>>?)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public SimpleTable()
    {
        InitializeComponent();
        Loaded += (_, _) => BuildTable();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimpleTable table && table.IsLoaded)
        {
            table.BuildTable();
        }
    }

    private void BuildTable()
    {
        var columns = Columns?.ToList();
        var rows = Rows?.ToList();

        if (columns is null || columns.Count == 0)
        {
            Content = null;
            return;
        }

        var headerStyle = (Style)Resources["SimpleTableHeaderStyle"];

        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 4,
        };

        for (var c = 0; c < columns.Count; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        // Header row
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var c = 0; c < columns.Count; c++)
        {
            var header = new TextBlock
            {
                Text = columns[c],
                Style = headerStyle,
            };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, c);
            grid.Children.Add(header);
        }

        if (rows is not null)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cells = rows[r].ToList();
                for (var c = 0; c < cells.Count && c < columns.Count; c++)
                {
                    var cell = new TextBlock
                    {
                        Text = cells[c],
                        IsTextSelectionEnabled = true,
                        TextWrapping = TextWrapping.NoWrap,
                    };
                    Grid.SetRow(cell, r + 1);
                    Grid.SetColumn(cell, c);
                    grid.Children.Add(cell);
                }
            }
        }

        Content = grid;
    }
}

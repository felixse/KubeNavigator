using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace KubeNavigator.Converters;

public partial class ListItemToMenuConverter : IValueConverter
{
    public ListItemToMenuConverter()
    {
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ISelectable selectable)
        {
            var menuFlyout = new MenuFlyout();
            foreach (var command in selectable.Commands)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = command.Name,
                    Command = command.Command,
                    Icon = new SymbolIcon(Enum.Parse<Symbol>(command.Symbol, true))
                };
                menuFlyout.Items.Add(menuItem);
            }
            return menuFlyout;
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

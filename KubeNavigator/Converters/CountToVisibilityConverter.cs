using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace KubeNavigator.Converters;

public partial class CountToVisibilityConverter : IValueConverter
{
    public int MinimumCount { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return count >= MinimumCount ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

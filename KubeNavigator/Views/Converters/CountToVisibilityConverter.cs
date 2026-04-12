using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KubeNavigator.Converters;

public partial class CountToVisibilityConverter : IValueConverter
{
    public int MinimumCount { get; set; }
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            var meetsThreshold = count >= MinimumCount;
            if (IsInverted) meetsThreshold = !meetsThreshold;
            return meetsThreshold ? Visibility.Visible : Visibility.Collapsed;
        }

        return IsInverted ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

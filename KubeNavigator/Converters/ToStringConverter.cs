using Microsoft.UI.Xaml.Data;
using System;

namespace KubeNavigator.Converters;
public partial class ToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() ?? "null";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

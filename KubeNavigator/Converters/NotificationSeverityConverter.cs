using KubeNavigator.Messages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace KubeNavigator.Converters;

public partial class NotificationSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NotificationSeverity severity)
        {
            return severity switch
            {
                NotificationSeverity.Success => InfoBarSeverity.Success,
                NotificationSeverity.Info => InfoBarSeverity.Informational,
                NotificationSeverity.Warning => InfoBarSeverity.Warning,
                NotificationSeverity.Error => InfoBarSeverity.Error,
                _ => throw new ValidationException("Unknown severity"),
            };
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

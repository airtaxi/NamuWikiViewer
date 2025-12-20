using Microsoft.UI.Xaml.Data;
using NamuWikiViewer.Commons.Models;
using System;

namespace NamuWikiViewer.Windows.Converters;

public class AppThemeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppTheme theme)
        {
            return theme switch
            {
                AppTheme.System => "시스템 테마",
                AppTheme.Light => "밝은 테마",
                AppTheme.Dark => "어두운 테마",
                _ => theme.ToString()
            };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

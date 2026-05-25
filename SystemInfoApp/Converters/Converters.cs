using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SystemInfoApp.Models;

namespace SystemInfoApp.Converters;

[ValueConversion(typeof(LoadingState), typeof(Brush))]
public sealed class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (LoadingState)value switch
        {
            LoadingState.Pending => new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x99)),
            LoadingState.Loading => new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)),
            LoadingState.Done    => new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x7A)),
            LoadingState.Error   => new SolidColorBrush(Color.FromRgb(0xFF, 0x4C, 0x4C)),
            _                   => Brushes.Gray
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(LoadingState), typeof(string))]
public sealed class StateToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (LoadingState)value switch
        {
            LoadingState.Pending => "○",
            LoadingState.Loading => "⟳",
            LoadingState.Done    => "✔",
            LoadingState.Error   => "✖",
            _                   => "?"
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool bv && bv;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(LoadingState), typeof(Visibility))]
public sealed class LoadingToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (LoadingState)value == LoadingState.Loading ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

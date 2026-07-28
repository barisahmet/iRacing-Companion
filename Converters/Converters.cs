using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IRacingSmartPlug.Converters;

/// <summary>bool → one of two brushes (True/False configurable in XAML).</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = Brushes.LimeGreen;
    public Brush FalseBrush { get; set; } = Brushes.Gray;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TrueBrush : FalseBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>HA connection state (0 unknown, 1 ok, 2 warn, 3 error) → brush.</summary>
public sealed class HaStateToBrushConverter : IValueConverter
{
    private static readonly Brush Ok = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
    private static readonly Brush Warn = new SolidColorBrush(Color.FromRgb(0xF0, 0xA6, 0x3B));
    private static readonly Brush Err = new SolidColorBrush(Color.FromRgb(0xFF, 0x5C, 0x52));
    private static readonly Brush Unknown = new SolidColorBrush(Color.FromRgb(0x8B, 0x93, 0xA1));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as int?) switch { 1 => Ok, 2 => Warn, 3 => Err, _ => Unknown };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Plug state code (1 on, 0 off, -1 unknown) → brush.</summary>
public sealed class PlugCodeToBrushConverter : IValueConverter
{
    private static readonly Brush On = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
    private static readonly Brush Off = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x63));
    private static readonly Brush Unknown = new SolidColorBrush(Color.FromRgb(0x8B, 0x93, 0xA1));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as int?) switch { 1 => On, 0 => Off, _ => Unknown };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>bool → Visibility (True = Visible). Set Invert=true to flip.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>bool → opacity (e.g. dim disabled rows). Configurable in XAML.</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    public double TrueOpacity { get; set; } = 1.0;
    public double FalseOpacity { get; set; } = 0.4;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TrueOpacity : FalseOpacity;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>Non-empty string → Visible (for inline notes).</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

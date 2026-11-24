using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Haushaltsbuch.Wpf.Converters;

/// <summary>
/// Liefert zu einem Monat (1–12) eine helle Pastellfarbe für den Zeilenhintergrund.
/// Die Farbtöne sind so verteilt, dass aufeinanderfolgende Monate deutlich verschieden aussehen
/// (Farbwinkel-Sprung von 150°), die Helligkeit bleibt hoch (≈ 86 %), damit dunkler Text gut lesbar ist.
/// </summary>
public sealed class MonthToBrushConverter : IValueConverter
{
    private static readonly Brush[] Brushes = new[]
    {
        "#F4C2C2", // Januar – Rot
        "#C2F4DB", // Februar – Mintgrün
        "#F4C2F4", // März – Magenta
        "#DBF4C2", // April – Gelbgrün
        "#C2C2F4", // Mai – Blau
        "#F4DBC2", // Juni – Orange
        "#C2F4F4", // Juli – Türkis
        "#F4C2DB", // August – Rosa
        "#C2F4C2", // September – Grün
        "#DBC2F4", // Oktober – Violett
        "#F4F4C2", // November – Gelb
        "#C2DBF4"  // Dezember – Azurblau
    }.Select(hex =>
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return (Brush)b;
    }).ToArray();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int month && month is >= 1 and <= 12 ? Brushes[month - 1] : System.Windows.Media.Brushes.White;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

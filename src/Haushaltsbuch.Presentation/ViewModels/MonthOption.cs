using System.Globalization;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Eintrag im Monatsfilter. <c>null/null</c> bedeutet „Alle Monate“.</summary>
public sealed record MonthOption(int? Year, int? Month, string Label)
{
    public static MonthOption All { get; } = new(null, null, "Alle Monate");

    public static MonthOption For(int year, int month, CultureInfo culture) =>
        new(year, month, new DateTime(year, month, 1).ToString("MMMM yyyy", culture));
}

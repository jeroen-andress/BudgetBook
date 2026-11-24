using System.Globalization;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Eintrag der Zeitraum-Auswahl in der Auswertung. <c>Month = null</c> bedeutet „Ganzes Jahr“.</summary>
public sealed record PeriodOption(int? Month, string Label)
{
    public static IReadOnlyList<PeriodOption> CreateAll(CultureInfo culture)
    {
        var list = new List<PeriodOption> { new(null, "Ganzes Jahr") };
        for (var month = 1; month <= 12; month++)
            list.Add(new PeriodOption(month, culture.DateTimeFormat.GetMonthName(month)));
        return list;
    }
}

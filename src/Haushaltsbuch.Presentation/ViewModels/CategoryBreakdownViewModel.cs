using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>
/// Eine Hälfte der Auswertung (Einnahmen ODER Ausgaben): Zeitraum-Auswahl und Kreisdiagramm-Daten.
/// </summary>
public sealed partial class CategoryBreakdownViewModel : ObservableObject
{
    private IReadOnlyList<Booking> _bookings = [];
    private IReadOnlyList<Category> _categories = [];
    private int _year;
    private readonly CultureInfo _culture;

    public CategoryBreakdownViewModel(string title, TransactionType type, CultureInfo culture)
    {
        _culture = culture;
        Title = title;
        Type = type;
        Periods = PeriodOption.CreateAll(culture);
        selectedPeriod = Periods[0];
    }

    public string Title { get; }
    public TransactionType Type { get; }
    public bool IsIncome => Type == TransactionType.Income;
    public IReadOnlyList<PeriodOption> Periods { get; }

    [ObservableProperty] private PeriodOption selectedPeriod;
    [ObservableProperty] private IReadOnlyList<PieSliceViewModel> slices = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalText))]
    private decimal total;

    /// <summary>Summe als Währungstext in der Kultur des Benutzers (z. B. „31.950,00 €“).</summary>
    public string TotalText => Total.ToString("C", _culture);
    [ObservableProperty] private bool hasData;

    partial void OnSelectedPeriodChanged(PeriodOption value) => Recalculate();

    /// <summary>Setzt die Datenbasis und berechnet das Diagramm neu.</summary>
    public void Update(IReadOnlyList<Booking> bookings, IReadOnlyList<Category> categories, int year)
    {
        _bookings = bookings;
        _categories = categories;
        _year = year;
        Recalculate();
    }

    private void Recalculate()
    {
        var totals = BreakdownCalculator.ByCategory(_bookings, Type, _year, SelectedPeriod?.Month);
        var sum = totals.Sum(t => t.Amount);

        // Farbe hängt an der Kategorie (nicht an der Position), damit sie in beiden Diagrammen
        // und bei jedem Zeitraum gleich bleibt.
        var colorByCategory = _categories.OrderBy(c => c.Id)
            .Select((c, index) => (c.Id, Color: ChartPalette.ForIndex(index)))
            .ToDictionary(x => x.Id, x => x.Color);
        var names = _categories.ToDictionary(c => c.Id, c => c.Name);

        Slices = totals
            .Select(t => new PieSliceViewModel(
                names.GetValueOrDefault(t.CategoryId, "–"),
                t.Amount,
                sum == 0 ? 0 : (double)(t.Amount / sum),
                colorByCategory.GetValueOrDefault(t.CategoryId, ChartPalette.ForIndex(0))))
            .ToList();
        Total = sum;
        HasData = totals.Count > 0;
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>ViewModel des Tabs „Auswertung“: links Einnahmen, rechts Ausgaben, je mit Kreisdiagramm.</summary>
public sealed partial class AnalysisViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly ICategoryService _categories;
    private readonly IDialogService _dialogs;
    private readonly Func<DateOnly> _today;

    private IReadOnlyList<Booking> _allBookings = [];
    private IReadOnlyList<Category> _allCategories = [];

    public AnalysisViewModel(
        IBookingService bookings,
        ICategoryService categories,
        IDialogService dialogs,
        CultureInfo? culture = null,
        Func<DateOnly>? today = null)
    {
        _bookings = bookings;
        _categories = categories;
        _dialogs = dialogs;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

        var c = culture ?? CultureInfo.CurrentCulture;
        Income = new CategoryBreakdownViewModel("Einnahmen", TransactionType.Income, c);
        Expenses = new CategoryBreakdownViewModel("Ausgaben", TransactionType.Expense, c);
        selectedYear = _today().Year;
        Years.Add(selectedYear);
    }

    public CategoryBreakdownViewModel Income { get; }
    public CategoryBreakdownViewModel Expenses { get; }

    /// <summary>Wählbare Jahre: alle Jahre mit Buchungen plus das aktuelle Jahr.</summary>
    public ObservableCollection<int> Years { get; } = [];

    [ObservableProperty] private int selectedYear;

    partial void OnSelectedYearChanged(int value) => UpdatePanels();

    public async Task RefreshAsync()
    {
        try
        {
            _allCategories = await _categories.GetAllAsync();
            _allBookings = await _bookings.GetAllAsync();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Die Auswertung konnte nicht geladen werden.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
            return;
        }

        var previous = SelectedYear;
        var years = _allBookings.Select(b => b.Date.Year).Append(_today().Year).Distinct()
            .OrderByDescending(y => y).ToList();

        // Liste abgleichen statt leeren: so verliert die ComboBox ihre Auswahl nicht.
        foreach (var obsolete in Years.Where(y => !years.Contains(y)).ToList()) Years.Remove(obsolete);
        foreach (var y in years)
            if (!Years.Contains(y)) Years.Insert(Math.Min(years.IndexOf(y), Years.Count), y);

        // Bisherige Auswahl behalten, sonst das aktuelle Jahr
        var target = years.Contains(previous) ? previous : _today().Year;
        if (SelectedYear == target) UpdatePanels();
        else SelectedYear = target;
    }

    private void UpdatePanels()
    {
        Income.Update(_allBookings, _allCategories, SelectedYear);
        Expenses.Update(_allBookings, _allCategories, SelectedYear);
    }
}

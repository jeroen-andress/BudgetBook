using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>
/// ViewModel des Tabs „Buchungen“: hält alle Buchungen, wendet Filter an, berechnet die Summen
/// und stellt die Befehle (Hinzufügen, Bearbeiten, Löschen, Export) bereit.
/// </summary>
public sealed partial class BookingsViewModel : ObservableObject
{
    private static readonly Category AllCategories = new(0, "Alle Kategorien");

    private readonly IBookingService _bookings;
    private readonly ICategoryService _categories;
    private readonly ICsvExporter _exporter;
    private readonly IDialogService _dialogs;
    private readonly CultureInfo _culture;

    private IReadOnlyList<Booking> _all = [];
    private IReadOnlyList<Category> _categoryList = [];
    private Dictionary<int, string> _categoryNames = new();
    private bool _suspendFilter;

    public BookingsViewModel(
        IBookingService bookings,
        ICategoryService categories,
        ICsvExporter exporter,
        IDialogService dialogs,
        CultureInfo? culture = null)
    {
        _bookings = bookings;
        _categories = categories;
        _exporter = exporter;
        _dialogs = dialogs;
        _culture = culture ?? CultureInfo.CurrentCulture;

        selectedMonth = MonthOption.All;
        selectedFilterCategory = AllCategories;
        selectedTypeOption = TypeOption.All;
    }

    // ---------- Daten für die View ----------

    public ObservableCollection<BookingRow> Rows { get; } = [];
    public ObservableCollection<MonthOption> MonthOptions { get; } = [];
    public ObservableCollection<Category> FilterCategories { get; } = [];
    public IReadOnlyList<TypeOption> TypeOptions { get; } = TypeOption.CreateAll();

    [ObservableProperty] private MonthOption selectedMonth;
    [ObservableProperty] private Category selectedFilterCategory;
    [ObservableProperty] private TypeOption selectedTypeOption;
    [ObservableProperty] private string searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(DeleteCommand))]
    private BookingRow? selectedRow;

    [ObservableProperty] private decimal totalIncome;
    [ObservableProperty] private decimal totalExpenses;
    [ObservableProperty] private decimal balance;

    /// <summary>Saldo über alle Buchungen – unabhängig vom aktiven Filter.</summary>
    [ObservableProperty] private decimal overallBalance;

    [ObservableProperty] private bool isFilterActive;

    // ---------- Initialisierung ----------

    /// <summary>Lädt Buchungen und Kategorien neu (Filterauswahl bleibt erhalten).</summary>
    public async Task RefreshAsync()
    {
        await RunSafeAsync(ReloadAsync, "Die Daten konnten nicht geladen werden.");
    }

    private async Task ReloadAsync()
    {
        var categories = await _categories.GetAllAsync();
        var bookings = await _bookings.GetAllAsync();

        _categoryList = categories;
        _categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);
        _all = bookings;

        var previousMonth = SelectedMonth;
        var previousCategoryId = SelectedFilterCategory.Id;

        _suspendFilter = true;
        try
        {
            RebuildMonthOptions(previousMonth);
            RebuildCategoryOptions(previousCategoryId);
        }
        finally
        {
            _suspendFilter = false;
        }

        ApplyFilter();
    }

    private void RebuildMonthOptions(MonthOption previous)
    {
        MonthOptions.Clear();
        MonthOptions.Add(MonthOption.All);
        foreach (var (year, month) in _all.Select(b => (b.Date.Year, b.Date.Month)).Distinct()
                     .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month))
            MonthOptions.Add(MonthOption.For(year, month, _culture));

        SelectedMonth = MonthOptions.FirstOrDefault(m => m.Year == previous.Year && m.Month == previous.Month)
                        ?? MonthOption.All;
    }

    private void RebuildCategoryOptions(int previousId)
    {
        FilterCategories.Clear();
        FilterCategories.Add(AllCategories);
        foreach (var c in _categoryList) FilterCategories.Add(c);

        SelectedFilterCategory = FilterCategories.FirstOrDefault(c => c.Id == previousId) ?? AllCategories;
    }

    // ---------- Filter ----------

    partial void OnSelectedMonthChanged(MonthOption value) => ApplyFilter();
    partial void OnSelectedFilterCategoryChanged(Category value) => ApplyFilter();
    partial void OnSelectedTypeOptionChanged(TypeOption value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private BookingFilter CurrentFilter => new(
        SelectedMonth?.Year,
        SelectedMonth?.Month,
        SelectedFilterCategory is { Id: > 0 } c ? c.Id : null,
        SearchText,
        SelectedTypeOption?.Type);

    private void ApplyFilter()
    {
        if (_suspendFilter) return;

        var filter = CurrentFilter;
        var visible = filter.Apply(_all, _categoryNames).ToList();

        var selectedId = SelectedRow?.Booking.Id;
        Rows.Clear();
        foreach (var b in visible)
            Rows.Add(new BookingRow(b, _categoryNames.GetValueOrDefault(b.CategoryId, "–")));
        SelectedRow = Rows.FirstOrDefault(r => r.Booking.Id == selectedId);

        var summary = BalanceCalculator.Calculate(visible);
        TotalIncome = summary.TotalIncome;
        TotalExpenses = summary.TotalExpenses;
        Balance = summary.Balance;
        OverallBalance = BalanceCalculator.Calculate(_all).Balance;
        IsFilterActive = filter.Year is not null || filter.CategoryId is not null || filter.Type is not null ||
                         !string.IsNullOrWhiteSpace(filter.SearchText);
    }

    [RelayCommand]
    private void ResetFilter()
    {
        _suspendFilter = true;
        SelectedMonth = MonthOption.All;
        SelectedFilterCategory = AllCategories;
        SelectedTypeOption = TypeOption.All;
        SearchText = string.Empty;
        _suspendFilter = false;
        ApplyFilter();
    }

    // ---------- Befehle ----------

    [RelayCommand]
    private async Task AddAsync()
    {
        if (_categoryList.Count == 0)
        {
            _dialogs.ShowInfo("Bitte legen Sie zuerst mindestens eine Kategorie an.");
            return;
        }

        var editor = new BookingEditorViewModel(_categoryList, null, _culture);
        if (!_dialogs.ShowBookingEditor(editor) || editor.Result is not { } booking) return;

        await RunSafeAsync(async () =>
        {
            await _bookings.AddAsync(booking);
            await ReloadAsync();
        }, "Die Buchung konnte nicht gespeichert werden.");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        if (SelectedRow is not { } row) return;

        var editor = new BookingEditorViewModel(_categoryList, row.Booking, _culture);
        if (!_dialogs.ShowBookingEditor(editor) || editor.Result is not { } booking) return;

        await RunSafeAsync(async () =>
        {
            await _bookings.UpdateAsync(booking);
            await ReloadAsync();
        }, "Die Buchung konnte nicht gespeichert werden.");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedRow is not { } row) return;

        var message = $"Buchung „{row.Description}“ vom {row.Date:d} wirklich löschen?";
        if (!_dialogs.Confirm(message, "Buchung löschen")) return;

        await RunSafeAsync(async () =>
        {
            await _bookings.DeleteAsync(row.Booking.Id);
            await ReloadAsync();
        }, "Die Buchung konnte nicht gelöscht werden.");
    }

    private bool HasSelection() => SelectedRow is not null;

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Rows.Count == 0)
        {
            _dialogs.ShowInfo("Es gibt keine Buchungen zum Exportieren.");
            return;
        }

        var path = _dialogs.PickCsvSavePath($"Haushaltsbuch_{DateTime.Today:yyyy-MM-dd}.csv");
        if (path is null) return;

        await RunSafeAsync(async () =>
        {
            await _exporter.ExportAsync(path, Rows.Select(r => r.Booking), _categoryNames);
            _dialogs.ShowInfo($"{Rows.Count} Buchungen wurden exportiert.");
        }, "Der Export ist fehlgeschlagen.");
    }

    // ---------- Fehlerbehandlung ----------

    /// <summary>Führt eine Aktion aus und zeigt Fehler dem Benutzer an, statt die Anwendung abstürzen zu lassen.</summary>
    private async Task RunSafeAsync(Func<Task> action, string errorPrefix)
    {
        try
        {
            await action();
        }
        catch (ValidationException ex)
        {
            _dialogs.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"{errorPrefix}{Environment.NewLine}{Environment.NewLine}{ex.Message}");
        }
    }
}

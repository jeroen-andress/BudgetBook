using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>ViewModel des Dialogs zum Erfassen bzw. Bearbeiten einer Buchung.</summary>
public sealed partial class BookingEditorViewModel : ObservableObject
{
    private readonly CultureInfo _culture;
    private readonly int _id;

    public BookingEditorViewModel(
        IReadOnlyList<Category> categories,
        Booking? existing = null,
        CultureInfo? culture = null,
        Func<DateOnly>? today = null)
    {
        _culture = culture ?? CultureInfo.CurrentCulture;
        Categories = categories;

        if (existing is null)
        {
            Title = "Neue Buchung";
            _id = 0;
            date = (today?.Invoke() ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
            isIncome = false;
            selectedCategory = categories.FirstOrDefault();
        }
        else
        {
            Title = "Buchung bearbeiten";
            _id = existing.Id;
            date = existing.Date.ToDateTime(TimeOnly.MinValue);
            description = existing.Description;
            amountText = existing.Amount.ToString("0.00", _culture);
            isIncome = existing.Type == TransactionType.Income;
            selectedCategory = categories.FirstOrDefault(c => c.Id == existing.CategoryId);
        }
    }

    public string Title { get; }
    public IReadOnlyList<Category> Categories { get; }

    [ObservableProperty] private DateTime? date;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private Category? selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense))]
    private bool isIncome;

    /// <summary>Gegenstück zu <see cref="IsIncome"/> für die zweite RadioButton.</summary>
    public bool IsExpense
    {
        get => !IsIncome;
        set => IsIncome = !value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string errorText = string.Empty;

    public bool HasErrors => !string.IsNullOrEmpty(ErrorText);

    /// <summary>Das Ergebnis nach erfolgreichem <see cref="Confirm"/>.</summary>
    public Booking? Result { get; private set; }

    /// <summary>Wird ausgelöst, wenn der Dialog geschlossen werden soll (true = bestätigt).</summary>
    public event Action<bool>? CloseRequested;

    /// <summary>Validiert die Eingaben und baut die Buchung. Gibt alle gefundenen Fehler zurück.</summary>
    public bool TryBuild(out Booking? booking, out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();
        booking = null;

        decimal amount = 0m;
        if (!AmountParser.TryParse(AmountText, _culture, out amount))
            problems.Add("Der Betrag ist keine gültige Zahl (Beispiel: 12,50).");

        if (Date is null)
            problems.Add("Bitte ein Datum angeben.");

        var candidate = new Booking(
            _id,
            Date is { } d ? DateOnly.FromDateTime(d) : default,
            (Description ?? string.Empty).Trim(),
            amount,
            SelectedCategory?.Id ?? 0,
            IsIncome ? TransactionType.Income : TransactionType.Expense);

        foreach (var e in BookingValidator.Validate(candidate))
        {
            // Doppelte Meldungen vermeiden: Betrag/Datum wurden oben schon gemeldet
            if (e.StartsWith("Der Betrag muss") && problems.Any(p => p.StartsWith("Der Betrag"))) continue;
            if (e == "Bitte ein Datum angeben." && problems.Contains(e)) continue;
            problems.Add(e);
        }

        errors = problems;
        if (problems.Count > 0) return false;

        booking = candidate;
        return true;
    }

    [RelayCommand]
    private void Confirm()
    {
        if (!TryBuild(out var booking, out var errors))
        {
            ErrorText = string.Join(Environment.NewLine, errors.Select(e => "• " + e));
            return;
        }

        ErrorText = string.Empty;
        Result = booking;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}

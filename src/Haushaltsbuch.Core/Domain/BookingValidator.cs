namespace Haushaltsbuch.Core.Domain;

/// <summary>Fachliche Validierung von Buchungen und Kategorien.</summary>
public static class BookingValidator
{
    public const int MaxDescriptionLength = 200;
    public const int MaxCategoryNameLength = 50;
    public const decimal MaxAmount = 1_000_000_000m;

    public static IReadOnlyList<string> Validate(Booking booking)
    {
        var errors = new List<string>();

        if (booking.Date == default)
            errors.Add("Bitte ein Datum angeben.");

        if (string.IsNullOrWhiteSpace(booking.Description))
            errors.Add("Bitte eine Beschreibung eingeben.");
        else if (booking.Description.Trim().Length > MaxDescriptionLength)
            errors.Add($"Die Beschreibung darf höchstens {MaxDescriptionLength} Zeichen lang sein.");

        if (booking.Amount <= 0)
            errors.Add("Der Betrag muss größer als 0 sein.");
        else if (booking.Amount > MaxAmount)
            errors.Add("Der Betrag ist zu groß.");
        else if (decimal.Round(booking.Amount, 2) != booking.Amount)
            errors.Add("Der Betrag darf höchstens zwei Nachkommastellen haben.");

        if (booking.CategoryId <= 0)
            errors.Add("Bitte eine Kategorie auswählen.");

        if (!Enum.IsDefined(booking.Type))
            errors.Add("Ungültiger Buchungstyp.");

        return errors;
    }

    public static IReadOnlyList<string> ValidateCategoryName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ["Bitte einen Kategorienamen eingeben."];
        if (name.Trim().Length > MaxCategoryNameLength)
            return [$"Der Kategoriename darf höchstens {MaxCategoryNameLength} Zeichen lang sein."];
        return [];
    }
}

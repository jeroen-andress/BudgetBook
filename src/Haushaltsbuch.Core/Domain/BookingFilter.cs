using System.Globalization;

namespace Haushaltsbuch.Core.Domain;

/// <summary>
/// Filterkriterien für Buchungen. Alle Kriterien sind optional und werden mit UND verknüpft.
/// </summary>
public sealed record BookingFilter(
    int? Year = null,
    int? Month = null,
    int? CategoryId = null,
    string? SearchText = null,
    TransactionType? Type = null)
{
    public static BookingFilter None { get; } = new();

    public bool Matches(Booking booking, string? categoryName = null)
    {
        if (Year is not null && booking.Date.Year != Year) return false;
        if (Month is not null && booking.Date.Month != Month) return false;
        if (CategoryId is not null && booking.CategoryId != CategoryId) return false;
        if (Type is not null && booking.Type != Type) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            var compare = CultureInfo.CurrentCulture.CompareInfo;
            var inDescription = compare.IndexOf(booking.Description, needle, CompareOptions.IgnoreCase) >= 0;
            var inCategory = categoryName is not null &&
                             compare.IndexOf(categoryName, needle, CompareOptions.IgnoreCase) >= 0;
            if (!inDescription && !inCategory) return false;
        }

        return true;
    }

    public IEnumerable<Booking> Apply(
        IEnumerable<Booking> bookings,
        IReadOnlyDictionary<int, string>? categoryNames = null) =>
        bookings.Where(b => Matches(b, categoryNames is not null && categoryNames.TryGetValue(b.CategoryId, out var n) ? n : null));
}

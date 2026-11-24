namespace Haushaltsbuch.Core.Domain;

/// <summary>Summe pro Kategorie.</summary>
public sealed record CategoryTotal(int CategoryId, decimal Amount);

/// <summary>Verteilung von Einnahmen bzw. Ausgaben auf Kategorien (Grundlage der Kreisdiagramme).</summary>
public static class BreakdownCalculator
{
    /// <param name="month">null = ganzes Jahr, sonst 1–12.</param>
    /// <returns>Kategorien mit Summe &gt; 0, absteigend nach Betrag sortiert.</returns>
    public static IReadOnlyList<CategoryTotal> ByCategory(
        IEnumerable<Booking> bookings, TransactionType type, int year, int? month = null)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        return bookings
            .Where(b => b.Type == type && b.Date.Year == year && (month is null || b.Date.Month == month))
            .GroupBy(b => b.CategoryId)
            .Select(g => new CategoryTotal(g.Key, g.Sum(b => b.Amount)))
            .Where(t => t.Amount > 0)
            .OrderByDescending(t => t.Amount)
            .ThenBy(t => t.CategoryId)
            .ToList();
    }
}

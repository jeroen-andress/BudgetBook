namespace Haushaltsbuch.Core.Domain;

/// <summary>Reine Berechnungslogik (keine Abhängigkeiten, leicht testbar).</summary>
public static class BalanceCalculator
{
    public static BalanceSummary Calculate(IEnumerable<Booking> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        decimal income = 0m, expenses = 0m;
        foreach (var b in bookings)
        {
            if (b.Type == TransactionType.Income) income += b.Amount;
            else expenses += b.Amount;
        }
        return new BalanceSummary(income, expenses);
    }
}

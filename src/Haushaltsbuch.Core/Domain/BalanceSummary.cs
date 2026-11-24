namespace Haushaltsbuch.Core.Domain;

/// <summary>Ergebnis einer Auswertung: Summe Einnahmen, Summe Ausgaben und Saldo.</summary>
public sealed record BalanceSummary(decimal TotalIncome, decimal TotalExpenses)
{
    public decimal Balance => TotalIncome - TotalExpenses;

    public static BalanceSummary Empty { get; } = new(0m, 0m);
}

using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Eintrag im Filter „Art“: alle, nur Einnahmen oder nur Ausgaben (<c>Type = null</c> = alle).</summary>
public sealed record TypeOption(TransactionType? Type, string Label)
{
    public static TypeOption All { get; } = new(null, "Alle");

    public static IReadOnlyList<TypeOption> CreateAll() =>
    [
        All,
        new(TransactionType.Income, "Einnahmen"),
        new(TransactionType.Expense, "Ausgaben")
    ];
}

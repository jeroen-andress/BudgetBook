namespace Haushaltsbuch.Core.Domain;

/// <summary>
/// Eine Buchung (Einnahme oder Ausgabe). Unveränderlich (record) – Änderungen erzeugen
/// per <c>with</c> eine neue Instanz. Der Betrag ist immer positiv, das Vorzeichen ergibt sich aus <see cref="Type"/>.
/// </summary>
public sealed record Booking(
    int Id,
    DateOnly Date,
    string Description,
    decimal Amount,
    int CategoryId,
    TransactionType Type)
{
    /// <summary>Betrag mit Vorzeichen: Einnahmen positiv, Ausgaben negativ.</summary>
    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;
}

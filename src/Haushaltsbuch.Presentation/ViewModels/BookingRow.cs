using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Darstellungsmodell einer Buchung in der Tabelle (Buchung + aufgelöster Kategoriename).</summary>
public sealed record BookingRow(Booking Booking, string CategoryName)
{
    public DateOnly Date => Booking.Date;

    /// <summary>Monat 1–12 – die View färbt die Zeile danach ein.</summary>
    public int Month => Booking.Date.Month;

    public string Description => Booking.Description;
    public decimal SignedAmount => Booking.SignedAmount;
    public bool IsIncome => Booking.Type == TransactionType.Income;
    public string TypeText => IsIncome ? "Einnahme" : "Ausgabe";
}

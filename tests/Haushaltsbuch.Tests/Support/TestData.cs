using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Tests.Support;

public static class TestData
{
    public static Booking Income(decimal amount, int categoryId = 1, string date = "2026-01-15", int id = 0) =>
        new(id, DateOnly.Parse(date), "Einnahme", amount, categoryId, TransactionType.Income);

    public static Booking Expense(decimal amount, int categoryId = 2, string date = "2026-01-20", int id = 0,
        string description = "Ausgabe") =>
        new(id, DateOnly.Parse(date), description, amount, categoryId, TransactionType.Expense);
}

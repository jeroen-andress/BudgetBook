using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class BreakdownCalculatorTests
{
    private static readonly Booking[] Data =
    [
        TestData.Income(2000m, categoryId: 1, date: "2026-01-31"),
        TestData.Income(2000m, categoryId: 1, date: "2026-02-28"),
        TestData.Income(150m, categoryId: 4, date: "2026-02-10"),
        TestData.Expense(800m, categoryId: 3, date: "2026-01-02"),
        TestData.Expense(45m, categoryId: 2, date: "2026-02-14"),
        TestData.Expense(30m, categoryId: 2, date: "2026-02-20"),
        TestData.Expense(99m, categoryId: 2, date: "2025-02-20")   // anderes Jahr
    ];

    [Fact]
    public void WholeYear_SumsPerCategory_SortedDescending()
    {
        var result = BreakdownCalculator.ByCategory(Data, TransactionType.Income, 2026);

        result.Should().Equal(new CategoryTotal(1, 4000m), new CategoryTotal(4, 150m));
    }

    [Fact]
    public void SingleMonth_OnlyThatMonth()
    {
        var result = BreakdownCalculator.ByCategory(Data, TransactionType.Income, 2026, month: 1);

        result.Should().Equal(new CategoryTotal(1, 2000m));
    }

    [Fact]
    public void Expenses_AreSeparatedFromIncome()
    {
        var result = BreakdownCalculator.ByCategory(Data, TransactionType.Expense, 2026);

        result.Should().Equal(new CategoryTotal(3, 800m), new CategoryTotal(2, 75m));
    }

    [Fact]
    public void OtherYears_AreIgnored() =>
        BreakdownCalculator.ByCategory(Data, TransactionType.Expense, 2025)
            .Should().Equal(new CategoryTotal(2, 99m));

    [Fact]
    public void NoMatches_ReturnsEmptyList() =>
        BreakdownCalculator.ByCategory(Data, TransactionType.Income, 2026, month: 7).Should().BeEmpty();

    [Fact]
    public void EqualAmounts_AreOrderedByCategoryIdForStability()
    {
        var data = new[] { TestData.Expense(10m, 5), TestData.Expense(10m, 2) };

        BreakdownCalculator.ByCategory(data, TransactionType.Expense, 2026)
            .Select(t => t.CategoryId).Should().Equal(2, 5);
    }
}

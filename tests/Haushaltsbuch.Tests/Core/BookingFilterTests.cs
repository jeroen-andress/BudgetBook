using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class BookingFilterTests
{
    private static readonly Booking[] Data =
    [
        TestData.Income(2000m, categoryId: 1, date: "2026-01-31", id: 1),
        TestData.Expense(800m, categoryId: 3, date: "2026-02-01", id: 2, description: "Miete Februar"),
        TestData.Expense(45m, categoryId: 2, date: "2026-02-14", id: 3, description: "Wocheneinkauf Rewe"),
        TestData.Expense(30m, categoryId: 2, date: "2025-02-03", id: 4, description: "Einkauf Vorjahr")
    ];

    private static readonly Dictionary<int, string> Names = new()
        { [1] = "Gehalt", [2] = "Lebensmittel", [3] = "Miete" };

    [Fact]
    public void None_ReturnsEverything() =>
        BookingFilter.None.Apply(Data).Should().HaveCount(4);

    [Fact]
    public void ByYearAndMonth_ReturnsOnlyMatchingMonth()
    {
        var result = new BookingFilter(Year: 2026, Month: 2).Apply(Data);

        result.Select(b => b.Id).Should().BeEquivalentTo([2, 3]);
    }

    [Fact]
    public void ByMonth_DistinguishesYears()
    {
        new BookingFilter(Year: 2025, Month: 2).Apply(Data).Select(b => b.Id).Should().BeEquivalentTo([4]);
    }

    [Fact]
    public void ByCategory_ReturnsOnlyThatCategory() =>
        new BookingFilter(CategoryId: 2).Apply(Data).Select(b => b.Id).Should().BeEquivalentTo([3, 4]);

    [Fact]
    public void Criteria_AreCombinedWithAnd() =>
        new BookingFilter(Year: 2026, Month: 2, CategoryId: 2).Apply(Data).Select(b => b.Id)
            .Should().BeEquivalentTo([3]);

    [Theory]
    [InlineData("rewe", new[] { 3 })]
    [InlineData("  MIETE ", new[] { 2 })]         // Groß-/Kleinschreibung und Leerzeichen egal
    [InlineData("einkauf", new[] { 3, 4 })]
    [InlineData("gibtesnicht", new int[0])]
    public void BySearchText_FindsInDescriptionAndCategory(string text, int[] expectedIds) =>
        new BookingFilter(SearchText: text).Apply(Data, Names).Select(b => b.Id).Should().BeEquivalentTo(expectedIds);

    [Fact]
    public void BySearchText_AlsoMatchesCategoryName() =>
        new BookingFilter(SearchText: "Gehalt").Apply(Data, Names).Select(b => b.Id).Should().BeEquivalentTo([1]);

    [Fact]
    public void ByType_Income_ReturnsOnlyIncome() =>
        new BookingFilter(Type: TransactionType.Income).Apply(Data).Select(b => b.Id).Should().BeEquivalentTo([1]);

    [Fact]
    public void ByType_Expense_ReturnsOnlyExpenses() =>
        new BookingFilter(Type: TransactionType.Expense).Apply(Data).Select(b => b.Id)
            .Should().BeEquivalentTo([2, 3, 4]);

    [Fact]
    public void ByType_IsCombinedWithOtherCriteria() =>
        new BookingFilter(Year: 2026, Month: 2, Type: TransactionType.Expense).Apply(Data).Select(b => b.Id)
            .Should().BeEquivalentTo([2, 3]);

    [Fact]
    public void BlankSearchText_IsIgnored() =>
        new BookingFilter(SearchText: "   ").Apply(Data).Should().HaveCount(4);
}

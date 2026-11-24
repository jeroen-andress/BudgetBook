using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class BalanceCalculatorTests
{
    [Fact]
    public void Calculate_WithNoBookings_ReturnsZero()
    {
        var result = BalanceCalculator.Calculate([]);

        result.TotalIncome.Should().Be(0m);
        result.TotalExpenses.Should().Be(0m);
        result.Balance.Should().Be(0m);
    }

    [Fact]
    public void Calculate_SumsIncomeAndExpensesSeparately()
    {
        var bookings = new[]
        {
            TestData.Income(2000m), TestData.Income(150.50m),
            TestData.Expense(800m), TestData.Expense(49.99m)
        };

        var result = BalanceCalculator.Calculate(bookings);

        result.TotalIncome.Should().Be(2150.50m);
        result.TotalExpenses.Should().Be(849.99m);
        result.Balance.Should().Be(1300.51m);
    }

    [Fact]
    public void Calculate_WhenExpensesExceedIncome_ReturnsNegativeBalance()
    {
        var result = BalanceCalculator.Calculate([TestData.Income(100m), TestData.Expense(250m)]);

        result.Balance.Should().Be(-150m);
    }

    [Fact]
    public void Calculate_DoesNotSufferFromFloatingPointErrors()
    {
        var bookings = Enumerable.Range(0, 10).Select(_ => TestData.Income(0.10m));

        BalanceCalculator.Calculate(bookings).Balance.Should().Be(1.00m);
    }

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        var act = () => BalanceCalculator.Calculate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

using System.Globalization;
using FluentAssertions;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Tests.Support;
using NSubstitute;
using Xunit;

namespace Haushaltsbuch.Tests.Presentation;

public class AnalysisViewModelTests
{
    private const int Gehalt = 1, Lebensmittel = 2, Miete = 3;

    private readonly InMemoryBookingRepository _bookingRepo = new();
    private readonly InMemoryCategoryRepository _categoryRepo = new(["Gehalt", "Lebensmittel", "Miete"]);
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private AnalysisViewModel Create() => new(
        new BookingService(_bookingRepo), new CategoryService(_categoryRepo), _dialogs,
        CultureInfo.GetCultureInfo("de-DE"), today: () => new DateOnly(2026, 6, 15));

    private async Task<AnalysisViewModel> CreateWithDataAsync()
    {
        await _bookingRepo.AddAsync(TestData.Income(2000m, Gehalt, "2026-01-31"));
        await _bookingRepo.AddAsync(TestData.Income(2000m, Gehalt, "2026-02-28"));
        await _bookingRepo.AddAsync(TestData.Expense(800m, Miete, "2026-01-02"));
        await _bookingRepo.AddAsync(TestData.Expense(60m, Lebensmittel, "2026-01-12"));
        await _bookingRepo.AddAsync(TestData.Expense(40m, Lebensmittel, "2026-02-14"));
        await _bookingRepo.AddAsync(TestData.Expense(10m, Lebensmittel, "2025-12-24"));
        var vm = Create();
        await vm.RefreshAsync();
        return vm;
    }

    [Fact]
    public void Periods_AreWholeYearPlusTwelveMonths()
    {
        var vm = Create();

        vm.Income.Periods.Should().HaveCount(13);
        vm.Income.Periods[0].Label.Should().Be("Ganzes Jahr");
        vm.Income.Periods[1].Label.Should().Be("Januar");
        vm.Income.Periods[12].Label.Should().Be("Dezember");
        vm.Income.SelectedPeriod.Month.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_OffersYearsWithDataAndCurrentYear_AndSelectsCurrentYear()
    {
        var vm = await CreateWithDataAsync();

        vm.Years.Should().Equal(2026, 2025);
        vm.SelectedYear.Should().Be(2026);
    }

    [Fact]
    public async Task Refresh_WithoutData_StillOffersCurrentYearAndShowsNothing()
    {
        var vm = Create();

        await vm.RefreshAsync();

        vm.Years.Should().Equal(2026);
        vm.Income.HasData.Should().BeFalse();
        vm.Expenses.HasData.Should().BeFalse();
        vm.Income.Slices.Should().BeEmpty();
    }

    [Fact]
    public async Task WholeYear_ShowsIncomeAndExpensesSeparately()
    {
        var vm = await CreateWithDataAsync();

        vm.Income.Total.Should().Be(4000m);
        vm.Income.Slices.Should().ContainSingle().Which.CategoryName.Should().Be("Gehalt");

        vm.Expenses.Total.Should().Be(900m);
        vm.Expenses.Slices.Select(s => s.CategoryName).Should().Equal("Miete", "Lebensmittel");
    }

    [Fact]
    public async Task TotalText_IsFormattedAsEuroInGermanCulture()
    {
        var vm = await CreateWithDataAsync();

        vm.Income.TotalText.Should().Be(4000m.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
        vm.Income.TotalText.Should().Contain("€").And.NotContain("$");
    }

    [Fact]
    public async Task TotalText_UpdatesWhenPeriodChanges()
    {
        var vm = await CreateWithDataAsync();

        vm.Expenses.SelectedPeriod = vm.Expenses.Periods.Single(p => p.Month == 2);

        vm.Expenses.TotalText.Should().Be(40m.ToString("C", CultureInfo.GetCultureInfo("de-DE")));
    }

    [Fact]
    public async Task SelectingMonth_RestrictsOnlyThatSide()
    {
        var vm = await CreateWithDataAsync();

        vm.Expenses.SelectedPeriod = vm.Expenses.Periods.Single(p => p.Month == 2);

        vm.Expenses.Total.Should().Be(40m);
        vm.Expenses.Slices.Should().ContainSingle().Which.CategoryName.Should().Be("Lebensmittel");
        vm.Income.Total.Should().Be(4000m); // linke Seite unverändert
    }

    [Fact]
    public async Task MonthWithoutBookings_HasNoData()
    {
        var vm = await CreateWithDataAsync();

        vm.Income.SelectedPeriod = vm.Income.Periods.Single(p => p.Month == 7);

        vm.Income.HasData.Should().BeFalse();
        vm.Income.Total.Should().Be(0m);
    }

    [Fact]
    public async Task Fractions_AddUpToOne()
    {
        var vm = await CreateWithDataAsync();

        vm.Expenses.Slices.Sum(s => s.Fraction).Should().BeApproximately(1.0, 1e-9);
        vm.Expenses.Slices[0].Fraction.Should().BeApproximately(800.0 / 900.0, 1e-9);
    }

    [Fact]
    public async Task ChangingYear_RecalculatesBothSides()
    {
        var vm = await CreateWithDataAsync();

        vm.SelectedYear = 2025;

        vm.Income.HasData.Should().BeFalse();
        vm.Expenses.Total.Should().Be(10m);
    }

    [Fact]
    public async Task CategoryKeepsItsColour_AcrossPeriodsAndSides()
    {
        var vm = await CreateWithDataAsync();
        var wholeYear = vm.Expenses.Slices.Single(s => s.CategoryName == "Lebensmittel").ColorHex;

        vm.Expenses.SelectedPeriod = vm.Expenses.Periods.Single(p => p.Month == 2);

        vm.Expenses.Slices.Single().ColorHex.Should().Be(wholeYear);
        vm.Expenses.Slices.Select(s => s.ColorHex).Distinct().Should().HaveCount(vm.Expenses.Slices.Count);
    }

    [Fact]
    public async Task Refresh_KeepsSelectedYearAndPeriods()
    {
        var vm = await CreateWithDataAsync();
        vm.SelectedYear = 2025;
        vm.Income.SelectedPeriod = vm.Income.Periods.Single(p => p.Month == 12);

        await _bookingRepo.AddAsync(TestData.Expense(5m, Lebensmittel, "2025-12-30"));
        await vm.RefreshAsync();

        vm.SelectedYear.Should().Be(2025);
        vm.Income.SelectedPeriod.Month.Should().Be(12);
        vm.Expenses.Total.Should().Be(15m);
    }

    [Fact]
    public async Task Refresh_WhenRepositoryFails_ShowsError()
    {
        _bookingRepo.FailWith = new IOException("Datenbank gesperrt");
        var vm = Create();

        await vm.RefreshAsync();

        _dialogs.Received(1).ShowError(Arg.Is<string>(m => m.Contains("Datenbank gesperrt")));
    }
}

using System.Globalization;
using FluentAssertions;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Tests.Support;
using NSubstitute;
using Xunit;

namespace Haushaltsbuch.Tests.Presentation;

public class BookingsViewModelTests
{
    private const int Gehalt = 1, Lebensmittel = 2, Miete = 3;

    private readonly InMemoryBookingRepository _bookingRepo = new();
    private readonly InMemoryCategoryRepository _categoryRepo = new(["Gehalt", "Lebensmittel", "Miete"]);
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ICsvExporter _exporter = Substitute.For<ICsvExporter>();

    private BookingsViewModel CreateViewModel() => new(
        new BookingService(_bookingRepo),
        new CategoryService(_categoryRepo),
        _exporter,
        _dialogs,
        CultureInfo.GetCultureInfo("de-DE"));

    /// <summary>Jan: +2000 Gehalt, -800 Miete · Feb: -45 Lebensmittel, -30 Lebensmittel.</summary>
    private async Task<BookingsViewModel> CreateWithSampleDataAsync()
    {
        await _bookingRepo.AddAsync(TestData.Income(2000m, Gehalt, "2026-01-31"));
        await _bookingRepo.AddAsync(TestData.Expense(800m, Miete, "2026-01-02", description: "Miete Januar"));
        await _bookingRepo.AddAsync(TestData.Expense(45m, Lebensmittel, "2026-02-14", description: "Rewe"));
        await _bookingRepo.AddAsync(TestData.Expense(30m, Lebensmittel, "2026-02-20", description: "Bäcker"));
        var vm = CreateViewModel();
        await vm.RefreshAsync();
        return vm;
    }

    [Fact]
    public async Task Initialize_LoadsRowsAndTotals()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.Rows.Should().HaveCount(4);
        vm.TotalIncome.Should().Be(2000m);
        vm.TotalExpenses.Should().Be(875m);
        vm.Balance.Should().Be(1125m);
        vm.OverallBalance.Should().Be(1125m);
    }

    [Fact]
    public async Task Initialize_ShowsOldestBookingFirst()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.Rows.Select(r => r.Description).Should().Equal("Miete Januar", "Einnahme", "Rewe", "Bäcker");
    }

    [Fact]
    public async Task Initialize_BuildsFilterOptions()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.MonthOptions.Select(m => m.Label).Should().Equal("Alle Monate", "Februar 2026", "Januar 2026");
        vm.FilterCategories.Select(c => c.Name).Should()
            .Equal("Alle Kategorien", "Gehalt", "Lebensmittel", "Miete");
    }

    [Fact]
    public async Task Initialize_WithEmptyDatabase_ShowsZeroes()
    {
        var vm = CreateViewModel();

        await vm.RefreshAsync();

        vm.Rows.Should().BeEmpty();
        vm.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task Initialize_WhenRepositoryFails_ShowsErrorInsteadOfCrashing()
    {
        _bookingRepo.FailWith = new IOException("Datenbank gesperrt");
        var vm = CreateViewModel();

        await vm.RefreshAsync();

        _dialogs.Received(1).ShowError(Arg.Is<string>(m => m.Contains("Datenbank gesperrt")));
    }

    [Fact]
    public async Task MonthFilter_RestrictsRowsAndTotals_ButNotOverallBalance()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 2);

        vm.Rows.Should().HaveCount(2);
        vm.TotalIncome.Should().Be(0m);
        vm.TotalExpenses.Should().Be(75m);
        vm.Balance.Should().Be(-75m);
        vm.OverallBalance.Should().Be(1125m);
        vm.IsFilterActive.Should().BeTrue();
    }

    [Fact]
    public async Task CategoryFilter_RestrictsRows()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedFilterCategory = vm.FilterCategories.Single(c => c.Id == Miete);

        vm.Rows.Should().ContainSingle().Which.Description.Should().Be("Miete Januar");
    }

    [Fact]
    public async Task SearchText_RestrictsRows()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SearchText = "bäck";

        vm.Rows.Should().ContainSingle().Which.Description.Should().Be("Bäcker");
    }

    [Fact]
    public async Task CombinedFilters_AreAppliedTogether()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 1);
        vm.SelectedFilterCategory = vm.FilterCategories.Single(c => c.Id == Lebensmittel);

        vm.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetFilter_ShowsEverythingAgain()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 2);
        vm.SearchText = "Rewe";

        vm.ResetFilterCommand.Execute(null);

        vm.Rows.Should().HaveCount(4);
        vm.SearchText.Should().BeEmpty();
        vm.SelectedMonth.Should().Be(MonthOption.All);
        vm.IsFilterActive.Should().BeFalse();
    }

    [Fact]
    public async Task Add_WhenConfirmed_SavesAndRefreshesTotals()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(ci =>
        {
            var editor = ci.Arg<BookingEditorViewModel>();
            editor.IsIncome = true;
            editor.Description = "Bonus";
            editor.AmountText = "500,00";
            editor.SelectedCategory = editor.Categories.Single(c => c.Id == Gehalt);
            editor.ConfirmCommand.Execute(null);
            return true;
        });

        await vm.AddCommand.ExecuteAsync(null);

        vm.Rows.Should().HaveCount(5);
        vm.TotalIncome.Should().Be(2500m);
        vm.Balance.Should().Be(1625m);
    }

    [Fact]
    public async Task Add_WhenCancelled_ChangesNothing()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(false);

        await vm.AddCommand.ExecuteAsync(null);

        vm.Rows.Should().HaveCount(4);
    }

    [Fact]
    public async Task Add_NewMonth_AppearsInMonthFilter()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(ci =>
        {
            var editor = ci.Arg<BookingEditorViewModel>();
            editor.Date = new DateTime(2026, 4, 1);
            editor.Description = "April";
            editor.AmountText = "10";
            editor.ConfirmCommand.Execute(null);
            return true;
        });

        await vm.AddCommand.ExecuteAsync(null);

        vm.MonthOptions.Select(m => m.Label).Should().Contain("April 2026");
    }

    [Fact]
    public async Task Add_KeepsActiveFilterAfterReload()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 2);
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(ci =>
        {
            var editor = ci.Arg<BookingEditorViewModel>();
            editor.Date = new DateTime(2026, 2, 25);
            editor.Description = "Noch ein Einkauf";
            editor.AmountText = "5";
            editor.ConfirmCommand.Execute(null);
            return true;
        });

        await vm.AddCommand.ExecuteAsync(null);

        vm.SelectedMonth.Month.Should().Be(2);
        vm.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Edit_WhenConfirmed_UpdatesBooking()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedRow = vm.Rows.Single(r => r.Description == "Rewe");
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(ci =>
        {
            var editor = ci.Arg<BookingEditorViewModel>();
            editor.AmountText = "55,00";
            editor.ConfirmCommand.Execute(null);
            return true;
        });

        await vm.EditCommand.ExecuteAsync(null);

        vm.Rows.Single(r => r.Description == "Rewe").SignedAmount.Should().Be(-55m);
        vm.TotalExpenses.Should().Be(885m);
    }

    [Fact]
    public async Task EditAndDelete_AreDisabledWithoutSelection()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.EditCommand.CanExecute(null).Should().BeFalse();
        vm.DeleteCommand.CanExecute(null).Should().BeFalse();

        vm.SelectedRow = vm.Rows.First();

        vm.EditCommand.CanExecute(null).Should().BeTrue();
        vm.DeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WhenConfirmed_RemovesBooking()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedRow = vm.Rows.Single(r => r.Description == "Miete Januar");
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.Rows.Should().HaveCount(3);
        vm.TotalExpenses.Should().Be(75m);
    }

    [Fact]
    public async Task Delete_WhenDeclined_KeepsBooking()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedRow = vm.Rows.First();
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.Rows.Should().HaveCount(4);
    }

    [Fact]
    public async Task Add_WhenSavingFails_ShowsError()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.ShowBookingEditor(Arg.Any<BookingEditorViewModel>()).Returns(ci =>
        {
            var editor = ci.Arg<BookingEditorViewModel>();
            editor.Description = "x";
            editor.AmountText = "1";
            editor.ConfirmCommand.Execute(null);
            return true;
        });
        _bookingRepo.FailWith = new IOException("Platte voll");

        await vm.AddCommand.ExecuteAsync(null);

        _dialogs.Received().ShowError(Arg.Is<string>(m => m.Contains("Platte voll")));
    }

    [Fact]
    public async Task Export_WithoutRows_ShowsInfoAndDoesNotAskForPath()
    {
        var vm = CreateViewModel();
        await vm.RefreshAsync();

        await vm.ExportCommand.ExecuteAsync(null);

        _dialogs.Received(1).ShowInfo(Arg.Any<string>());
        _dialogs.DidNotReceive().PickCsvSavePath(Arg.Any<string>());
    }

    [Fact]
    public async Task Export_ExportsOnlyFilteredRows()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 2);
        _dialogs.PickCsvSavePath(Arg.Any<string>()).Returns("/tmp/export.csv");

        await vm.ExportCommand.ExecuteAsync(null);

        await _exporter.Received(1).ExportAsync(
            "/tmp/export.csv",
            Arg.Is<IEnumerable<Booking>>(b => b.Count() == 2),
            Arg.Any<IReadOnlyDictionary<int, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_WhenUserCancelsFileDialog_DoesNothing()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.PickCsvSavePath(Arg.Any<string>()).Returns((string?)null);

        await vm.ExportCommand.ExecuteAsync(null);

        await _exporter.DidNotReceiveWithAnyArgs().ExportAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Export_WhenWritingFails_ShowsError()
    {
        var vm = await CreateWithSampleDataAsync();
        _dialogs.PickCsvSavePath(Arg.Any<string>()).Returns("/nicht/erlaubt.csv");
        _exporter.ExportAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromException(new UnauthorizedAccessException("Zugriff verweigert")));

        await vm.ExportCommand.ExecuteAsync(null);

        _dialogs.Received().ShowError(Arg.Is<string>(m => m.Contains("Zugriff verweigert")));
    }

    [Fact]
    public async Task Rows_ExposeTheMonthUsedForRowColouring()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.Rows.Single(r => r.Description == "Rewe").Month.Should().Be(2);
        vm.Rows.Single(r => r.Description == "Miete Januar").Month.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_PicksUpCategoriesAddedElsewhere()
    {
        var vm = await CreateWithSampleDataAsync();
        await _categoryRepo.AddAsync("Reisen");

        await vm.RefreshAsync();

        vm.FilterCategories.Select(c => c.Name).Should().Contain("Reisen");
    }

    [Fact]
    public async Task TypeOptions_OfferAllIncomeAndExpenses()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.TypeOptions.Select(t => t.Label).Should().Equal("Alle", "Einnahmen", "Ausgaben");
        vm.SelectedTypeOption.Should().Be(TypeOption.All);
    }

    [Fact]
    public async Task TypeFilter_Income_ShowsOnlyIncomeAndUpdatesTotals()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedTypeOption = vm.TypeOptions.Single(t => t.Type == TransactionType.Income);

        vm.Rows.Should().ContainSingle().Which.Description.Should().Be("Einnahme");
        vm.TotalIncome.Should().Be(2000m);
        vm.TotalExpenses.Should().Be(0m);
        vm.IsFilterActive.Should().BeTrue();
        vm.OverallBalance.Should().Be(1125m);
    }

    [Fact]
    public async Task TypeFilter_Expense_ShowsOnlyExpenses()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedTypeOption = vm.TypeOptions.Single(t => t.Type == TransactionType.Expense);

        vm.Rows.Should().HaveCount(3).And.OnlyContain(r => !r.IsIncome);
        vm.TotalExpenses.Should().Be(875m);
        vm.Balance.Should().Be(-875m);
    }

    [Fact]
    public async Task TypeFilter_CombinesWithMonthFilter()
    {
        var vm = await CreateWithSampleDataAsync();

        vm.SelectedMonth = vm.MonthOptions.Single(m => m.Month == 1);
        vm.SelectedTypeOption = vm.TypeOptions.Single(t => t.Type == TransactionType.Expense);

        vm.Rows.Should().ContainSingle().Which.Description.Should().Be("Miete Januar");
    }

    [Fact]
    public async Task TypeFilter_SurvivesReload()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedTypeOption = vm.TypeOptions.Single(t => t.Type == TransactionType.Income);

        await vm.RefreshAsync();

        vm.SelectedTypeOption.Type.Should().Be(TransactionType.Income);
        vm.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task ResetFilter_AlsoResetsType()
    {
        var vm = await CreateWithSampleDataAsync();
        vm.SelectedTypeOption = vm.TypeOptions.Single(t => t.Type == TransactionType.Income);

        vm.ResetFilterCommand.Execute(null);

        vm.SelectedTypeOption.Should().Be(TypeOption.All);
        vm.Rows.Should().HaveCount(4);
        vm.IsFilterActive.Should().BeFalse();
    }
}

using System.Globalization;
using FluentAssertions;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Tests.Support;
using NSubstitute;
using Xunit;

namespace Haushaltsbuch.Tests.Presentation;

public class ShellViewModelTests
{
    private readonly InMemoryBookingRepository _bookingRepo = new();
    private readonly InMemoryCategoryRepository _categoryRepo = new(["Gehalt", "Miete"]);
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        var dialogs = Substitute.For<IDialogService>();
        var bookings = new BookingService(_bookingRepo);
        var categories = new CategoryService(_categoryRepo);
        var de = CultureInfo.GetCultureInfo("de-DE");

        _shell = new ShellViewModel(
            new BookingsViewModel(bookings, categories, Substitute.For<ICsvExporter>(), dialogs, de),
            new AnalysisViewModel(bookings, categories, dialogs, de, () => new DateOnly(2026, 6, 1)),
            new SettingsViewModel(categories, dialogs));
    }

    [Fact]
    public void StartsOnBookingsTab() => _shell.SelectedTabIndex.Should().Be(ShellViewModel.BookingsTab);

    [Fact]
    public async Task Initialize_LoadsBookingsTab()
    {
        await _bookingRepo.AddAsync(TestData.Income(100m, 1, "2026-01-01"));

        await _shell.InitializeAsync();

        _shell.Bookings.Rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task RefreshAnalysisTab_ShowsNewlyAddedBookings()
    {
        await _shell.RefreshTabAsync(ShellViewModel.AnalysisTab);
        _shell.Analysis.Income.HasData.Should().BeFalse();

        await _bookingRepo.AddAsync(TestData.Income(100m, 1, "2026-03-01"));
        await _shell.RefreshTabAsync(ShellViewModel.AnalysisTab);

        _shell.Analysis.Income.Total.Should().Be(100m);
    }

    [Fact]
    public async Task CategoryAddedInSettings_AppearsInBookingsFilterAfterRefresh()
    {
        await _shell.RefreshTabAsync(ShellViewModel.SettingsTab);
        _shell.Settings.NewName = "Reisen";
        await _shell.Settings.AddCommand.ExecuteAsync(null);

        await _shell.RefreshTabAsync(ShellViewModel.BookingsTab);

        _shell.Bookings.FilterCategories.Select(c => c.Name).Should().Contain("Reisen");
    }

    [Fact]
    public async Task RefreshTab_WithUnknownIndex_DoesNothing()
    {
        var act = () => _shell.RefreshTabAsync(99);

        await act.Should().NotThrowAsync();
    }
}

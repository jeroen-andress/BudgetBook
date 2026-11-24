using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Tests.Support;
using NSubstitute;
using Xunit;

namespace Haushaltsbuch.Tests.Presentation;

public class SettingsViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private async Task<SettingsViewModel> CreateAsync(Func<int, bool>? isInUse = null)
    {
        var service = new CategoryService(new InMemoryCategoryRepository(["Miete", "Freizeit"], isInUse));
        var vm = new SettingsViewModel(service, _dialogs);
        await vm.RefreshAsync();
        return vm;
    }

    [Fact]
    public async Task Add_ValidName_AppendsAndMarksChanged()
    {
        var vm = await CreateAsync();
        vm.NewName = "Reisen";

        await vm.AddCommand.ExecuteAsync(null);

        vm.Categories.Select(c => c.Name).Should().Contain("Reisen");
        vm.NewName.Should().BeEmpty();
        vm.StatusText.Should().Contain("Reisen");
        vm.ErrorText.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_LoadsCategoriesSortedByName()
    {
        var vm = await CreateAsync();

        vm.Categories.Select(c => c.Name).Should().Equal("Freizeit", "Miete");
    }

    [Fact]
    public async Task Add_NewCategory_IsPersistedAndSurvivesReload()
    {
        var vm = await CreateAsync();
        vm.NewName = "Reisen";
        await vm.AddCommand.ExecuteAsync(null);

        await vm.RefreshAsync(); // wie nach einem Neustart: Daten kommen aus dem Repository

        vm.Categories.Select(c => c.Name).Should().Equal("Freizeit", "Miete", "Reisen");
    }

    [Fact]
    public async Task Add_IsDisabledForBlankName()
    {
        var vm = await CreateAsync();

        vm.AddCommand.CanExecute(null).Should().BeFalse();
        vm.NewName = "x";
        vm.AddCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Add_Duplicate_ShowsErrorAndKeepsList()
    {
        var vm = await CreateAsync();
        vm.NewName = "miete";

        await vm.AddCommand.ExecuteAsync(null);

        vm.ErrorText.Should().Contain("existiert bereits");
        vm.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_WhenConfirmed_RemovesCategory()
    {
        var vm = await CreateAsync();
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        vm.SelectedCategory = vm.Categories.First(c => c.Name == "Freizeit");

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.Categories.Select(c => c.Name).Should().Equal("Miete");
        vm.StatusText.Should().Contain("gelöscht");
    }

    [Fact]
    public async Task Delete_WhenDeclined_KeepsCategory()
    {
        var vm = await CreateAsync();
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        vm.SelectedCategory = vm.Categories[0];

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_CategoryInUse_ShowsError()
    {
        var vm = await CreateAsync(isInUse: _ => true);
        _dialogs.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        vm.SelectedCategory = vm.Categories[0];

        await vm.DeleteCommand.ExecuteAsync(null);

        vm.ErrorText.Should().Contain("verwendet");
        vm.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_IsDisabledWithoutSelection()
    {
        var vm = await CreateAsync();

        vm.DeleteCommand.CanExecute(null).Should().BeFalse();
    }
}

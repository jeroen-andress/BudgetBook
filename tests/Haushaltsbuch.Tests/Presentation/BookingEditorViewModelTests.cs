using System.Globalization;
using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Presentation;

public class BookingEditorViewModelTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly IReadOnlyList<Category> Categories = [new(1, "Gehalt"), new(2, "Lebensmittel")];

    private static BookingEditorViewModel NewEditor(Booking? existing = null) =>
        new(Categories, existing, German, today: () => new DateOnly(2026, 5, 10));

    private static void Fill(BookingEditorViewModel vm, string description = "Brot", string amount = "3,50")
    {
        vm.Description = description;
        vm.AmountText = amount;
        vm.SelectedCategory = Categories[1];
    }

    [Fact]
    public void NewBooking_StartsWithTodayAndExpenseAndFirstCategory()
    {
        var vm = NewEditor();

        vm.Title.Should().Be("Neue Buchung");
        vm.Date.Should().Be(new DateTime(2026, 5, 10));
        vm.IsExpense.Should().BeTrue();
        vm.IsIncome.Should().BeFalse();
        vm.SelectedCategory.Should().Be(Categories[0]);
    }

    [Fact]
    public void ExistingBooking_PrefillsAllFields()
    {
        var existing = new Booking(42, new DateOnly(2026, 2, 3), "Gehalt Feb", 2500m, 1, TransactionType.Income);

        var vm = NewEditor(existing);

        vm.Title.Should().Be("Buchung bearbeiten");
        vm.Description.Should().Be("Gehalt Feb");
        vm.AmountText.Should().Be("2500,00");
        vm.IsIncome.Should().BeTrue();
        vm.SelectedCategory.Should().Be(Categories[0]);
    }

    [Fact]
    public void Confirm_WithValidInput_BuildsResultAndRequestsClose()
    {
        var vm = NewEditor();
        Fill(vm);
        bool? closedWith = null;
        vm.CloseRequested += ok => closedWith = ok;

        vm.ConfirmCommand.Execute(null);

        closedWith.Should().BeTrue();
        vm.HasErrors.Should().BeFalse();
        vm.Result.Should().Be(new Booking(0, new DateOnly(2026, 5, 10), "Brot", 3.50m, 2, TransactionType.Expense));
    }

    [Fact]
    public void Confirm_WhenEditing_KeepsTheBookingId()
    {
        var vm = NewEditor(new Booking(42, new DateOnly(2026, 2, 3), "Alt", 1m, 1, TransactionType.Expense));
        vm.Description = "Neu";

        vm.ConfirmCommand.Execute(null);

        vm.Result!.Id.Should().Be(42);
        vm.Result.Description.Should().Be("Neu");
    }

    [Fact]
    public void Confirm_WithEmptyDescription_ShowsErrorAndDoesNotClose()
    {
        var vm = NewEditor();
        Fill(vm, description: "  ");
        var closed = false;
        vm.CloseRequested += _ => closed = true;

        vm.ConfirmCommand.Execute(null);

        closed.Should().BeFalse();
        vm.Result.Should().BeNull();
        vm.HasErrors.Should().BeTrue();
        vm.ErrorText.Should().Contain("Beschreibung");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("12,5€")]
    public void Confirm_WithNonNumericAmount_ShowsSingleAmountError(string amount)
    {
        var vm = NewEditor();
        Fill(vm, amount: amount);

        vm.ConfirmCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.ErrorText.Should().Contain("keine gültige Zahl");
        vm.ErrorText.Should().NotContain("größer als 0");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4,20")]
    public void Confirm_WithNonPositiveAmount_ShowsError(string amount)
    {
        var vm = NewEditor();
        Fill(vm, amount: amount);

        vm.ConfirmCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.ErrorText.Should().Contain("größer als 0");
    }

    [Fact]
    public void Confirm_WithoutDate_ShowsErrorOnce()
    {
        var vm = NewEditor();
        Fill(vm);
        vm.Date = null;

        vm.ConfirmCommand.Execute(null);

        vm.Result.Should().BeNull();
        vm.ErrorText.Split("Datum").Length.Should().Be(2); // genau eine Meldung
    }

    [Fact]
    public void Confirm_WithoutCategory_ShowsError()
    {
        var vm = NewEditor();
        Fill(vm);
        vm.SelectedCategory = null;

        vm.ConfirmCommand.Execute(null);

        vm.ErrorText.Should().Contain("Kategorie");
    }

    [Fact]
    public void Confirm_AfterFixingInput_ClearsError()
    {
        var vm = NewEditor();
        Fill(vm, amount: "abc");
        vm.ConfirmCommand.Execute(null);
        vm.HasErrors.Should().BeTrue();

        vm.AmountText = "5";
        vm.ConfirmCommand.Execute(null);

        vm.HasErrors.Should().BeFalse();
        vm.Result.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_RequestsCloseWithFalse()
    {
        var vm = NewEditor();
        bool? closedWith = null;
        vm.CloseRequested += ok => closedWith = ok;

        vm.CancelCommand.Execute(null);

        closedWith.Should().BeFalse();
        vm.Result.Should().BeNull();
    }

    [Fact]
    public void TogglingIsIncome_UpdatesIsExpense()
    {
        var vm = NewEditor();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsIncome = true;

        vm.IsExpense.Should().BeFalse();
        changed.Should().Contain(nameof(vm.IsExpense));
    }
}

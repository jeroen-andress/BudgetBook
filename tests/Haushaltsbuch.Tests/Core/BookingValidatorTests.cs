using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class BookingValidatorTests
{
    private static Booking Valid => TestData.Expense(12.50m);

    [Fact]
    public void ValidBooking_HasNoErrors() =>
        BookingValidator.Validate(Valid).Should().BeEmpty();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveAmount_IsRejected(double amount) =>
        BookingValidator.Validate(Valid with { Amount = (decimal)amount })
            .Should().ContainSingle(e => e.Contains("größer als 0"));

    [Fact]
    public void MoreThanTwoDecimals_IsRejected() =>
        BookingValidator.Validate(Valid with { Amount = 1.234m })
            .Should().ContainSingle(e => e.Contains("Nachkommastellen"));

    [Fact]
    public void TooLargeAmount_IsRejected() =>
        BookingValidator.Validate(Valid with { Amount = BookingValidator.MaxAmount + 1 })
            .Should().ContainSingle(e => e.Contains("zu groß"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDescription_IsRejected(string description) =>
        BookingValidator.Validate(Valid with { Description = description })
            .Should().ContainSingle(e => e.Contains("Beschreibung"));

    [Fact]
    public void TooLongDescription_IsRejected() =>
        BookingValidator.Validate(Valid with { Description = new string('x', 201) })
            .Should().ContainSingle(e => e.Contains("200"));

    [Fact]
    public void MissingCategory_IsRejected() =>
        BookingValidator.Validate(Valid with { CategoryId = 0 })
            .Should().ContainSingle(e => e.Contains("Kategorie"));

    [Fact]
    public void DefaultDate_IsRejected() =>
        BookingValidator.Validate(Valid with { Date = default })
            .Should().ContainSingle(e => e.Contains("Datum"));

    [Fact]
    public void InvalidType_IsRejected() =>
        BookingValidator.Validate(Valid with { Type = (TransactionType)42 })
            .Should().ContainSingle(e => e.Contains("Buchungstyp"));

    [Fact]
    public void MultipleProblems_AreAllReported() =>
        BookingValidator.Validate(Valid with { Amount = 0, Description = "", CategoryId = 0 })
            .Should().HaveCount(3);

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void CategoryName_Blank_IsRejected(string? name) =>
        BookingValidator.ValidateCategoryName(name).Should().NotBeEmpty();

    [Fact]
    public void CategoryName_Valid_HasNoErrors() =>
        BookingValidator.ValidateCategoryName("Reisen").Should().BeEmpty();
}

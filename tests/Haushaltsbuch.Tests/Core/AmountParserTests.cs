using System.Globalization;
using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class AmountParserTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("  7 ", 7)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("0,99", 0.99)]
    public void German_ValidInput_IsParsed(string input, double expected)
    {
        AmountParser.TryParse(input, German, out var amount).Should().BeTrue();
        amount.Should().Be((decimal)expected);
    }

    [Fact]
    public void NegativeNumber_IsParsedSoValidationCanExplainTheProblem()
    {
        AmountParser.TryParse("-4,20", German, out var amount).Should().BeTrue();
        amount.Should().Be(-4.20m);
    }

    [Fact]
    public void English_UsesDotAsDecimalSeparator()
    {
        AmountParser.TryParse("12.50", English, out var amount).Should().BeTrue();
        amount.Should().Be(12.50m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12,5€")]
    [InlineData("1e5")]
    public void InvalidInput_IsRejected(string? input) =>
        AmountParser.TryParse(input, German, out _).Should().BeFalse();
}

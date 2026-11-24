using FluentAssertions;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class CsvFormatterTests
{
    private static readonly Dictionary<int, string> Names = new() { [1] = "Gehalt", [2] = "Lebensmittel" };

    [Fact]
    public void Format_WritesHeaderAndRowsWithGermanFormat()
    {
        var csv = CsvFormatter.Format(
            [TestData.Income(1234.5m, 1, "2026-03-01"), TestData.Expense(9.99m, 2, "2026-03-02", description: "Brot")],
            Names);

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("Datum;Typ;Kategorie;Beschreibung;Betrag");
        lines[1].Should().Be("01.03.2026;Einnahme;Gehalt;Einnahme;1234,50");
        lines[2].Should().Be("02.03.2026;Ausgabe;Lebensmittel;Brot;-9,99");
    }

    [Fact]
    public void Format_EscapesSemicolonsAndQuotes()
    {
        var csv = CsvFormatter.Format([TestData.Expense(1m, 2, description: "Brot; \"frisch\"")], Names);

        csv.Should().Contain("\"Brot; \"\"frisch\"\"\"");
    }

    [Fact]
    public void Format_NeutralizesFormulaInjection()
    {
        var csv = CsvFormatter.Format([TestData.Expense(1m, 2, description: "=SUM(A1:A9)")], Names);

        csv.Should().Contain(";'=SUM(A1:A9);");
    }

    [Fact]
    public void Format_UnknownCategory_YieldsEmptyField()
    {
        var csv = CsvFormatter.Format([TestData.Expense(1m, categoryId: 99)], Names);

        csv.Should().Contain(";Ausgabe;;Ausgabe;");
    }
}

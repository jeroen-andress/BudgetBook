using System.Globalization;
using System.Text;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Services;

/// <summary>Erzeugt CSV-Text (Semikolon-getrennt, deutsches Zahlenformat – direkt in Excel (DE) öffnbar).</summary>
public static class CsvFormatter
{
    public static string Format(IEnumerable<Booking> bookings, IReadOnlyDictionary<int, string> categoryNames)
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        var sb = new StringBuilder();
        sb.AppendLine("Datum;Typ;Kategorie;Beschreibung;Betrag");

        foreach (var b in bookings)
        {
            var category = categoryNames.TryGetValue(b.CategoryId, out var n) ? n : string.Empty;
            sb.Append(b.Date.ToString("dd.MM.yyyy", de)).Append(';')
              .Append(b.Type == TransactionType.Income ? "Einnahme" : "Ausgabe").Append(';')
              .Append(Escape(category)).Append(';')
              .Append(Escape(b.Description)).Append(';')
              .Append(b.SignedAmount.ToString("0.00", de))
              .AppendLine();
        }
        return sb.ToString();
    }

    private static string Escape(string value)
    {
        // Schutz vor CSV-/Formel-Injection in Excel
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return value.IndexOfAny([';', '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}

using System.Globalization;

namespace Haushaltsbuch.Core.Domain;

/// <summary>Wandelt Benutzereingaben (z. B. "1.234,56" oder "12,5") in einen Betrag um.</summary>
public static class AmountParser
{
    public static bool TryParse(string? input, IFormatProvider culture, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;

        return decimal.TryParse(
            input.Trim(),
            // Vorzeichen erlauben: "-4,20" ist eine gültige Zahl und wird erst von der Validierung
            // mit der passenden Meldung ("größer als 0") abgelehnt.
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            culture,
            out amount);
    }
}

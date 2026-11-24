namespace Haushaltsbuch.Core.Domain;

/// <summary>Kategorie einer Buchung (z. B. Gehalt, Miete).</summary>
public sealed record Category(int Id, string Name);

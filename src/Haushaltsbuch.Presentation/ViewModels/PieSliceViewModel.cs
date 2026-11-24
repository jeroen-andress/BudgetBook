namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Ein Tortenstück bzw. Legendeneintrag: Kategorie, Betrag, Anteil (0–1) und Farbe.</summary>
public sealed record PieSliceViewModel(string CategoryName, decimal Amount, double Fraction, string ColorHex);

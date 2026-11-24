namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>Gedeckte, gut unterscheidbare Diagrammfarben (hex).</summary>
public static class ChartPalette
{
    public static readonly IReadOnlyList<string> Colors =
    [
        "#4E79A7", "#F28E2B", "#E15759", "#76B7B2", "#59A14F", "#EDC948",
        "#B07AA1", "#FF9DA7", "#9C755F", "#BAB0AC", "#86BCB6", "#D37295"
    ];

    public static string ForIndex(int index) => Colors[((index % Colors.Count) + Colors.Count) % Colors.Count];
}

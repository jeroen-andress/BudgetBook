using Haushaltsbuch.Presentation.ViewModels;

namespace Haushaltsbuch.Presentation.Services;

/// <summary>
/// Abstraktion für alle Dialoge. Die ViewModels kennen keine WPF-Typen – so bleiben sie testbar.
/// Die WPF-Implementierung liegt im Wpf-Projekt.
/// </summary>
public interface IDialogService
{
    /// <summary>Zeigt den Buchungs-Editor modal. Liefert true, wenn bestätigt wurde.</summary>
    bool ShowBookingEditor(BookingEditorViewModel editor);

    bool Confirm(string message, string title);

    void ShowError(string message);

    void ShowInfo(string message);

    /// <summary>Datei-Speichern-Dialog. Liefert null bei Abbruch.</summary>
    string? PickCsvSavePath(string suggestedFileName);
}

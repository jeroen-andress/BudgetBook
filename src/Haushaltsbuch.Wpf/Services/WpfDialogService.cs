using System.Windows;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Wpf.Views;
using Microsoft.Win32;

namespace Haushaltsbuch.Wpf.Services;

/// <summary>WPF-Implementierung der Dialoge. Einzige Stelle, die Fenster und MessageBoxen kennt.</summary>
public sealed class WpfDialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    public bool ShowBookingEditor(BookingEditorViewModel editor) =>
        new BookingEditorWindow(editor) { Owner = Owner }.ShowDialog() == true;


    public bool Confirm(string message, string title) =>
        MessageBox.Show(Owner!, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowError(string message) =>
        MessageBox.Show(Owner!, message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message) =>
        MessageBox.Show(Owner!, message, "Haushaltsbuch", MessageBoxButton.OK, MessageBoxImage.Information);

    public string? PickCsvSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Buchungen exportieren",
            Filter = "CSV-Datei (*.csv)|*.csv",
            FileName = suggestedFileName,
            DefaultExt = ".csv"
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }
}

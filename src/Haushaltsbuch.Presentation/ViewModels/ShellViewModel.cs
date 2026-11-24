using CommunityToolkit.Mvvm.ComponentModel;

namespace Haushaltsbuch.Presentation.ViewModels;

/// <summary>
/// ViewModel des Hauptfensters: hält die drei Tabs und lädt die Daten eines Tabs neu,
/// sobald er ausgewählt wird (so sind z. B. neue Kategorien oder Buchungen überall aktuell).
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    public const int BookingsTab = 0;
    public const int AnalysisTab = 1;
    public const int SettingsTab = 2;

    public ShellViewModel(BookingsViewModel bookings, AnalysisViewModel analysis, SettingsViewModel settings)
    {
        Bookings = bookings;
        Analysis = analysis;
        Settings = settings;
    }

    public BookingsViewModel Bookings { get; }
    public AnalysisViewModel Analysis { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty] private int selectedTabIndex = BookingsTab;

    // Fire-and-forget ist hier ok: die Tab-ViewModels fangen ihre Fehler selbst ab.
    partial void OnSelectedTabIndexChanged(int value) => _ = RefreshTabAsync(value);

    public Task InitializeAsync() => RefreshTabAsync(SelectedTabIndex);

    public Task RefreshTabAsync(int tabIndex) => tabIndex switch
    {
        BookingsTab => Bookings.RefreshAsync(),
        AnalysisTab => Analysis.RefreshAsync(),
        SettingsTab => Settings.RefreshAsync(),
        _ => Task.CompletedTask
    };
}

using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Infrastructure.Persistence;
using Haushaltsbuch.Presentation.Services;
using Haushaltsbuch.Presentation.ViewModels;
using Haushaltsbuch.Wpf.Services;
using Haushaltsbuch.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Haushaltsbuch.Wpf;

/// <summary>
/// Composition Root: Hier – und nur hier – werden alle Schichten per Dependency Injection verdrahtet.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // WPF verwendet für Bindings standardmäßig en-US; so werden Datum/Währung im System-Format angezeigt.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Unerwarteter Fehler:{Environment.NewLine}{args.Exception.Message}",
                "Haushaltsbuch", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            _services = ConfigureServices().BuildServiceProvider();

            await _services.GetRequiredService<SqliteDatabase>().InitializeAsync();

            var shell = _services.GetRequiredService<ShellViewModel>();
            // Explizit setzen: Run-Elemente (z. B. „Summe: …“) erben die Sprache nur von gesetzten Werten,
            // nicht vom Standard – sonst würde dort die Währung als „$“ formatiert.
            var window = new MainWindow
            {
                DataContext = shell,
                Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)
            };
            MainWindow = window;
            window.Show();

            await shell.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Die Anwendung konnte nicht gestartet werden:{Environment.NewLine}{ex.Message}",
                "Haushaltsbuch", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static IServiceCollection ConfigureServices()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Haushaltsbuch", "haushaltsbuch.db");

        var services = new ServiceCollection();

        // Infrastruktur
        services.AddSingleton(new SqliteDatabase(dbPath));
        services.AddSingleton<IBookingRepository, SqliteBookingRepository>();
        services.AddSingleton<ICategoryRepository, SqliteCategoryRepository>();
        services.AddSingleton<ICsvExporter, CsvFileExporter>();

        // Anwendungslogik
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<ICategoryService, CategoryService>();

        // Präsentation
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<BookingsViewModel>();
        services.AddSingleton<AnalysisViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}

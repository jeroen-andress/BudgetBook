# Haushaltsbuch (WPF · C# · .NET 9)

Desktop-Anwendung zur Verwaltung privater Einnahmen und Ausgaben.

## Starten

Voraussetzungen: Windows 10/11, Visual Studio 2022 (17.12+) mit Workload „.NET-Desktopentwicklung“ und .NET 9 SDK.

1. `Haushaltsbuch.sln` in Visual Studio öffnen (NuGet-Pakete werden automatisch wiederhergestellt).
2. Startprojekt **Haushaltsbuch.Wpf** wählen und mit **F5** starten.
3. Tests: *Test → Test-Explorer → Alle Tests ausführen* oder `dotnet test` im Projektordner.

Die Daten liegen in `%LocalAppData%\Haushaltsbuch\haushaltsbuch.db` (SQLite, meist `C:\Users\<Name>\AppData\Local\Haushaltsbuch\`; zum Zurücksetzen die Datei bei geschlossener App löschen) und bleiben beim Neustart erhalten.
Beim ersten Start werden die Kategorien Gehalt, Lebensmittel, Miete, Freizeit und Sonstiges angelegt.

## Funktionen

Navigation über Tabs in der linken Seitenleiste (mit Icons):

* **Buchungen** – Tabelle mit Zeilenfarbe je Monat (dezente Pastelltöne), Filter, Suche, CSV-Export
* **Auswertung** – links Einnahmen, rechts Ausgaben; je ein Kreisdiagramm nach Kategorien für „Ganzes Jahr“ oder einen Monat (das Jahr wählt man oben)
* **Einstellungen** – eigene Kategorien anlegen/löschen (in der SQLite-Datenbank gespeichert)

Details:

* Buchungen erfassen, bearbeiten (Doppelklick), löschen (Entf) – mit Datum, Beschreibung, Betrag, Kategorie, Typ
* Tabelle mit Summe Einnahmen, Summe Ausgaben, Saldo (aktuelle Ansicht) und Gesamtsaldo (alle Buchungen)
* Filter nach Monat, Kategorie und Art (Einnahmen/Ausgaben), dazu Suche in Beschreibung/Kategorie
* Kategorien mit Buchungen sind vor dem Löschen geschützt
* CSV-Export der aktuell gefilterten Buchungen (Excel-tauglich, `;`-getrennt)
* Demodaten: `demo-daten.sql` (Januar – November 2026) lässt sich in SQLite einspielen, siehe Kommentar am Dateianfang
* Fehlerbehandlung: ungültige Eingaben werden im Dialog erklärt, Datenbankfehler als Meldung angezeigt

## Architektur

Schichtenarchitektur („Clean Architecture light“) mit **MVVM** in der Präsentationsschicht.
Abhängigkeiten zeigen immer nach innen – der Kern kennt weder WPF noch SQLite.

```
Haushaltsbuch.Wpf  ──►  Haushaltsbuch.Presentation  ──►  Haushaltsbuch.Core  ◄──  Haushaltsbuch.Infrastructure
(Views, DI, Dialoge)    (ViewModels, IDialogService)      (Domäne, Services,        (SQLite, CSV-Datei)
                                                           Repository-Interfaces)
```

| Projekt | Zielframework | Inhalt |
|---|---|---|
| `Core` | net9.0 | Entitäten (`Booking`, `Category`), Berechnung (`BalanceCalculator`), Filter, Validierung, Services, Repository-Interfaces |
| `Infrastructure` | net9.0 | SQLite-Repositories (`Microsoft.Data.Sqlite`), CSV-Export |
| `Presentation` | net9.0 | ViewModels (CommunityToolkit.Mvvm), `IDialogService` – **ohne WPF-Abhängigkeit** |
| `Wpf` | net9.0-windows | XAML-Views, `WpfDialogService`, Composition Root (`App.xaml.cs`) |
| `Tests` | net9.0 | xUnit, FluentAssertions, NSubstitute |

**Warum so?**

* **Shell + Tab-ViewModels:** `ShellViewModel` hält `BookingsViewModel`, `AnalysisViewModel` und `SettingsViewModel`. Beim Wechsel eines Tabs lädt dieser seine Daten neu – so sind neue Kategorien/Buchungen überall aktuell, ohne dass sich die ViewModels kennen.
* **Kreisdiagramm ohne Fremdbibliothek:** Die Berechnung (`BreakdownCalculator`) liegt im Core und ist getestet; das `PieChart`-Control zeichnet nur noch (WPF `StreamGeometry`).
* **MVVM:** Views (XAML) enthalten keine Logik, nur Bindings. Alles Verhalten steckt in ViewModels mit `ObservableProperty` und `RelayCommand`.
* **ViewModels in eigenem Projekt ohne WPF:** Dialoge laufen über `IDialogService`; dadurch sind ViewModels vollständig ohne UI testbar (und auch außerhalb von Windows).
* **Dependency Inversion:** Der Kern definiert `IBookingRepository` & Co.; SQLite ist austauschbar. In Tests ersetzen In-Memory-Repositories die Datenbank.
* **Dependency Injection:** `App.xaml.cs` ist der einzige Ort, an dem Klassen konkret verdrahtet werden (`Microsoft.Extensions.DependencyInjection`).
* **Unveränderliche Domänenobjekte** (`record`): Änderungen erzeugen neue Instanzen, das vermeidet versteckte Seiteneffekte.
* **Geldbeträge** sind `decimal` und werden in SQLite als ganze Cent (INTEGER) gespeichert – keine Rundungsfehler.
* **Validierung zweistufig:** Das ViewModel meldet Eingabefehler sofort im Dialog, der `BookingService` validiert zusätzlich fachlich (nie nur der UI vertrauen).
* **Fehlerbehandlung:** `ValidationException` für fachliche Fehler; unerwartete Fehler fängt das ViewModel ab und zeigt sie an, statt abzustürzen.

## Tests

Rund 120 Unit- und Integrationstests:

* `Core/` – Saldoberechnung, Kategorie-Auswertung, Filter, Validierung, Betrags-Parser, Services, CSV
* `Infrastructure/` – echte SQLite-Datei: CRUD, Neustart-Persistenz, Fremdschlüssel, Eindeutigkeit
* `Presentation/` – Shell-, Buchungs-, Editor-, Auswertungs- und Einstellungs-ViewModel inkl. Fehlerfälle

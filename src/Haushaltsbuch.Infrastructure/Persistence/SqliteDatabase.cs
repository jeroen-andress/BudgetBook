using Microsoft.Data.Sqlite;

namespace Haushaltsbuch.Infrastructure.Persistence;

/// <summary>
/// Kapselt Verbindungsaufbau und Schema-Initialisierung der SQLite-Datenbank.
/// Jede Operation öffnet eine kurzlebige Verbindung (SQLite pooled diese intern).
/// </summary>
public sealed class SqliteDatabase
{
    private static readonly string[] DefaultCategories = ["Gehalt", "Lebensmittel", "Miete", "Freizeit", "Sonstiges"];

    private readonly string _connectionString;

    public SqliteDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            Pooling = false // Datei bleibt nach Gebrauch nicht gesperrt (wichtig für Tests und Backups)
        }.ToString();
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    /// <summary>Legt Tabellen an und befüllt Beispielkategorien, falls die Datenbank neu ist.</summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);

        await using (var create = connection.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS Categories (
                    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE COLLATE NOCASE
                );
                CREATE TABLE IF NOT EXISTS Bookings (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date        TEXT    NOT NULL,            -- ISO 8601 (yyyy-MM-dd)
                    Description TEXT    NOT NULL,
                    AmountCents INTEGER NOT NULL CHECK (AmountCents > 0),
                    CategoryId  INTEGER NOT NULL REFERENCES Categories(Id),
                    Type        INTEGER NOT NULL CHECK (Type IN (0, 1))
                );
                CREATE INDEX IF NOT EXISTS IX_Bookings_Date ON Bookings(Date);
                """;
            await create.ExecuteNonQueryAsync(ct);
        }

        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Categories";
        var existing = Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        if (existing > 0) return;

        foreach (var name in DefaultCategories)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Categories (Name) VALUES ($name)";
            insert.Parameters.AddWithValue("$name", name);
            await insert.ExecuteNonQueryAsync(ct);
        }
    }
}

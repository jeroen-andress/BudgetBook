using System.Globalization;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;
using Microsoft.Data.Sqlite;

namespace Haushaltsbuch.Infrastructure.Persistence;

public sealed class SqliteBookingRepository(SqliteDatabase database) : IBookingRepository
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Date, Description, AmountCents, CategoryId, Type FROM Bookings";

        var result = new List<Booking>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Add(new Booking(
                Id: reader.GetInt32(0),
                Date: DateOnly.ParseExact(reader.GetString(1), DateFormat, CultureInfo.InvariantCulture),
                Description: reader.GetString(2),
                Amount: reader.GetInt64(3) / 100m,
                CategoryId: reader.GetInt32(4),
                Type: (TransactionType)reader.GetInt32(5)));
        }
        return result;
    }

    public async Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Bookings (Date, Description, AmountCents, CategoryId, Type)
            VALUES ($date, $description, $cents, $categoryId, $type);
            SELECT last_insert_rowid();
            """;
        BindValues(cmd, booking);
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return booking with { Id = id };
    }

    public async Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Bookings
            SET Date = $date, Description = $description, AmountCents = $cents,
                CategoryId = $categoryId, Type = $type
            WHERE Id = $id
            """;
        BindValues(cmd, booking);
        cmd.Parameters.AddWithValue("$id", booking.Id);
        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            throw new KeyNotFoundException($"Buchung {booking.Id} wurde nicht gefunden.");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Bookings WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void BindValues(SqliteCommand cmd, Booking b)
    {
        cmd.Parameters.AddWithValue("$date", b.Date.ToString(DateFormat, CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$description", b.Description);
        cmd.Parameters.AddWithValue("$cents", (long)decimal.Round(b.Amount * 100m, 0, MidpointRounding.AwayFromZero));
        cmd.Parameters.AddWithValue("$categoryId", b.CategoryId);
        cmd.Parameters.AddWithValue("$type", (int)b.Type);
    }
}

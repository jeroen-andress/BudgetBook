using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Infrastructure.Persistence;

public sealed class SqliteCategoryRepository(SqliteDatabase database) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Categories ORDER BY Name";

        var result = new List<Category>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new Category(reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    public async Task<Category> AddAsync(string name, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Categories (Name) VALUES ($name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", name);
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return new Category(id, name);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Categories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await database.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM Bookings WHERE CategoryId = $id)";
        cmd.Parameters.AddWithValue("$id", id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)) == 1;
    }
}

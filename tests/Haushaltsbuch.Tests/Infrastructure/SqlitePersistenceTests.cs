using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Infrastructure.Persistence;
using Haushaltsbuch.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Haushaltsbuch.Tests.Infrastructure;

/// <summary>Integrationstests gegen eine echte SQLite-Datei im Temp-Verzeichnis.</summary>
public sealed class SqlitePersistenceTests : IAsyncLifetime, IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"haushaltsbuch-test-{Guid.NewGuid():N}.db");
    private SqliteDatabase _db = null!;

    public async Task InitializeAsync()
    {
        _db = new SqliteDatabase(_path);
        await _db.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public async Task Initialize_SeedsDefaultCategories()
    {
        var categories = await new SqliteCategoryRepository(_db).GetAllAsync();

        categories.Select(c => c.Name).Should()
            .BeEquivalentTo("Gehalt", "Lebensmittel", "Miete", "Freizeit", "Sonstiges");
    }

    [Fact]
    public async Task Initialize_CalledTwice_DoesNotDuplicateCategories()
    {
        await _db.InitializeAsync();

        (await new SqliteCategoryRepository(_db).GetAllAsync()).Should().HaveCount(5);
    }

    [Fact]
    public async Task Add_AssignsIdAndRoundTripsAllFields()
    {
        var repo = new SqliteBookingRepository(_db);
        var categoryId = (await new SqliteCategoryRepository(_db).GetAllAsync()).First().Id;

        var saved = await repo.AddAsync(new Booking(0, new DateOnly(2026, 3, 14), "Wocheneinkauf äöü", 12.34m,
            categoryId, TransactionType.Expense));

        saved.Id.Should().BeGreaterThan(0);
        var loaded = (await repo.GetAllAsync()).Single();
        loaded.Should().Be(saved);
        loaded.Amount.Should().Be(12.34m);
    }

    [Fact]
    public async Task Data_SurvivesRestart()
    {
        var categoryId = (await new SqliteCategoryRepository(_db).GetAllAsync()).First().Id;
        await new SqliteBookingRepository(_db).AddAsync(TestData.Income(500m, categoryId));

        // "Neustart": neue Datenbank-Instanz auf derselben Datei
        var restartedDb = new SqliteDatabase(_path);
        await restartedDb.InitializeAsync();

        (await new SqliteBookingRepository(restartedDb).GetAllAsync()).Should().ContainSingle()
            .Which.Amount.Should().Be(500m);
        (await new SqliteCategoryRepository(restartedDb).GetAllAsync()).Should().HaveCount(5);
    }

    [Fact]
    public async Task Update_ChangesStoredValues()
    {
        var repo = new SqliteBookingRepository(_db);
        var categoryId = (await new SqliteCategoryRepository(_db).GetAllAsync()).First().Id;
        var saved = await repo.AddAsync(TestData.Expense(10m, categoryId));

        await repo.UpdateAsync(saved with { Description = "Geändert", Amount = 99.99m, Type = TransactionType.Income });

        var loaded = (await repo.GetAllAsync()).Single();
        loaded.Description.Should().Be("Geändert");
        loaded.Amount.Should().Be(99.99m);
        loaded.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task Update_UnknownId_Throws()
    {
        var repo = new SqliteBookingRepository(_db);

        await repo.Invoking(r => r.UpdateAsync(TestData.Expense(1m, 1, id: 12345)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Delete_RemovesBooking()
    {
        var repo = new SqliteBookingRepository(_db);
        var categoryId = (await new SqliteCategoryRepository(_db).GetAllAsync()).First().Id;
        var saved = await repo.AddAsync(TestData.Expense(10m, categoryId));

        await repo.DeleteAsync(saved.Id);

        (await repo.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Add_WithUnknownCategory_IsRejectedByForeignKey()
    {
        var repo = new SqliteBookingRepository(_db);

        await repo.Invoking(r => r.AddAsync(TestData.Expense(10m, categoryId: 9999)))
            .Should().ThrowAsync<SqliteException>();
    }

    [Fact]
    public async Task IsInUse_ReflectsBookings()
    {
        var categories = new SqliteCategoryRepository(_db);
        var all = await categories.GetAllAsync();
        var used = all[0].Id;
        var unused = all[1].Id;
        await new SqliteBookingRepository(_db).AddAsync(TestData.Expense(5m, used));

        (await categories.IsInUseAsync(used)).Should().BeTrue();
        (await categories.IsInUseAsync(unused)).Should().BeFalse();
    }

    [Fact]
    public async Task Category_DuplicateNameIgnoringCase_IsRejectedByUniqueConstraint()
    {
        var categories = new SqliteCategoryRepository(_db);

        await categories.Invoking(c => c.AddAsync("miete")).Should().ThrowAsync<SqliteException>();
    }
}

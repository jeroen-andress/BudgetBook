using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Tests.Support;

public sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly List<Booking> _items = [];
    private int _nextId = 1;

    /// <summary>Wenn gesetzt, wirft jede Operation diese Exception (Fehlerfall-Tests).</summary>
    public Exception? FailWith { get; set; }

    public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default)
    {
        if (FailWith is not null) throw FailWith;
        return Task.FromResult<IReadOnlyList<Booking>>(_items.ToList());
    }

    public Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        if (FailWith is not null) throw FailWith;
        var saved = booking with { Id = _nextId++ };
        _items.Add(saved);
        return Task.FromResult(saved);
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        if (FailWith is not null) throw FailWith;
        var index = _items.FindIndex(b => b.Id == booking.Id);
        if (index < 0) throw new KeyNotFoundException();
        _items[index] = booking;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (FailWith is not null) throw FailWith;
        _items.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _items = [];
    private readonly Func<int, bool> _isInUse;
    private int _nextId = 1;

    public InMemoryCategoryRepository(IEnumerable<string> names, Func<int, bool>? isInUse = null)
    {
        _isInUse = isInUse ?? (_ => false);
        foreach (var n in names) _items.Add(new Category(_nextId++, n));
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Category>>(_items.ToList());

    public Task<Category> AddAsync(string name, CancellationToken ct = default)
    {
        var c = new Category(_nextId++, name);
        _items.Add(c);
        return Task.FromResult(c);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _items.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken ct = default) => Task.FromResult(_isInUse(id));
}

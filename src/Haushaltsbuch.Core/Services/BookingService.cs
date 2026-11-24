using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default);
    Task<Booking> AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Anwendungsfälle rund um Buchungen: validiert und delegiert an das Repository.</summary>
public sealed class BookingService(IBookingRepository repository) : IBookingService
{
    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default) =>
        (await repository.GetAllAsync(ct))
            .OrderBy(b => b.Date)
            .ThenBy(b => b.Id)
            .ToList();

    public Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        var normalized = Normalize(booking);
        EnsureValid(normalized);
        return repository.AddAsync(normalized, ct);
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        var normalized = Normalize(booking);
        EnsureValid(normalized);
        return repository.UpdateAsync(normalized, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => repository.DeleteAsync(id, ct);

    private static Booking Normalize(Booking b) => b with { Description = b.Description?.Trim() ?? string.Empty };

    private static void EnsureValid(Booking booking)
    {
        var errors = BookingValidator.Validate(booking);
        if (errors.Count > 0) throw new ValidationException(errors);
    }
}

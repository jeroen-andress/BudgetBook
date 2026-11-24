using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Abstractions;

/// <summary>Persistenz-Abstraktion für Buchungen (Implementierung in der Infrastruktur-Schicht).</summary>
public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default);
    Task<Booking> AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

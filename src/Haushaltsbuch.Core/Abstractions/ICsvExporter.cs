using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Abstractions;

public interface ICsvExporter
{
    Task ExportAsync(string path, IEnumerable<Booking> bookings, IReadOnlyDictionary<int, string> categoryNames,
        CancellationToken ct = default);
}

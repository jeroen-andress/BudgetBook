using System.Text;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;

namespace Haushaltsbuch.Infrastructure.Persistence;

public sealed class CsvFileExporter : ICsvExporter
{
    public Task ExportAsync(string path, IEnumerable<Booking> bookings,
        IReadOnlyDictionary<int, string> categoryNames, CancellationToken ct = default) =>
        // UTF-8 mit BOM, damit Excel Umlaute korrekt erkennt
        File.WriteAllTextAsync(path, CsvFormatter.Format(bookings, categoryNames), new UTF8Encoding(true), ct);
}

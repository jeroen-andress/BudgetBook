using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category> AddAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> IsInUseAsync(int id, CancellationToken ct = default);
}

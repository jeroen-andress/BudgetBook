using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;

namespace Haushaltsbuch.Core.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category> AddAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        (await repository.GetAllAsync(ct)).OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public async Task<Category> AddAsync(string name, CancellationToken ct = default)
    {
        var errors = BookingValidator.ValidateCategoryName(name);
        if (errors.Count > 0) throw new ValidationException(errors);

        var trimmed = name.Trim();
        var existing = await repository.GetAllAsync(ct);
        if (existing.Any(c => string.Equals(c.Name, trimmed, StringComparison.CurrentCultureIgnoreCase)))
            throw new ValidationException($"Die Kategorie „{trimmed}“ existiert bereits.");

        return await repository.AddAsync(trimmed, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (await repository.IsInUseAsync(id, ct))
            throw new ValidationException("Die Kategorie wird noch von Buchungen verwendet und kann nicht gelöscht werden.");
        await repository.DeleteAsync(id, ct);
    }
}

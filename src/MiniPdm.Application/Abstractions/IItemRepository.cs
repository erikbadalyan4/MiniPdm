using MiniPdm.Domain.Entities;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Application.Abstractions;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Item?> GetByDesignationAsync(Designation designation, CancellationToken ct = default);
    Task<Item?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Item>> SearchAsync(string query, CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct = default);
    Task UpdateAsync(Item item, CancellationToken ct = default);
    Task SaveVersionAsync(ItemVersion version, CancellationToken ct = default);
    Task SaveBomLinksAsync(Guid parentVersionId, IEnumerable<BomLink> links, CancellationToken ct = default);
    Task<ItemVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct = default);
    Task<IReadOnlyList<BomLink>> GetBomLinksByVersionIdAsync(Guid versionId, CancellationToken ct = default);
}


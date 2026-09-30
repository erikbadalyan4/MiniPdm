using MiniPdm.Application.Models.Bom;

namespace MiniPdm.Application.Abstractions;

public interface IBomQueryRepository
{
    /// <summary>
    /// Извлекает полное дерево состава изделия рекурсивным CTE-запросом.
    /// </summary>
    Task<IReadOnlyList<BomHierarchyNode>> GetFullHierarchyAsync(Guid rootObjectId, CancellationToken ct = default);

    /// <summary>
    /// Извлекает состав изделия первого уровня для версии сборки.
    /// </summary>
    Task<IReadOnlyList<BomHierarchyNode>> GetFirstLevelComponentsAsync(Guid assemblyVersionId, CancellationToken ct = default);
}


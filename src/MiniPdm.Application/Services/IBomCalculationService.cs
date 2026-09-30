using MiniPdm.Application.Models.Bom;

namespace MiniPdm.Application.Services;

public interface IBomCalculationService
{
    /// <summary>
    /// Вычисляет суммарную массу сборки по узлам иерархии с учетом количеств и вложенностей.
    /// Если хотя бы у одного компонента не задана масса, возвращает список таких компонентов.
    /// </summary>
    MassCalculationResult CalculateMass(IReadOnlyList<BomHierarchyNode> hierarchyNodes);

    /// <summary>
    /// Формирует сводную спецификацию: плоский список всех деталей и стандартных изделий
    /// с суммарным перемноженным количеством по всему дереву.
    /// </summary>
    IReadOnlyList<ConsolidatedBomItem> BuildConsolidatedSpecification(IReadOnlyList<BomHierarchyNode> hierarchyNodes);

    /// <summary>
    /// Вычисляет массу сборки из репозитория по ее корневому идентификатору.
    /// </summary>
    Task<MassCalculationResult> CalculateAssemblyMassAsync(Guid assemblyId, CancellationToken ct = default);

    /// <summary>
    /// Формирует сводную спецификацию сборки из репозитория по ее корневому идентификатору.
    /// </summary>
    Task<IReadOnlyList<ConsolidatedBomItem>> GetConsolidatedSpecificationAsync(Guid assemblyId, CancellationToken ct = default);
}

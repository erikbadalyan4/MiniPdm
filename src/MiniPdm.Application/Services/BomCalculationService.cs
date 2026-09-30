using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Services;

public sealed class BomCalculationService : IBomCalculationService
{
    private readonly IBomQueryRepository? _queryRepository;

    public BomCalculationService(IBomQueryRepository? queryRepository = null)
    {
        _queryRepository = queryRepository;
    }

    public MassCalculationResult CalculateMass(IReadOnlyList<BomHierarchyNode> hierarchyNodes)
    {
        if (hierarchyNodes.Count == 0)
            return MassCalculationResult.Success(0m);

        var consolidated = BuildConsolidatedSpecification(hierarchyNodes);

        var missing = new List<string>();
        decimal totalMass = 0m;

        foreach (var item in consolidated)
        {
            if (!item.UnitMassKg.HasValue)
            {
                var label = item.Designation.HasValue
                    ? $"{item.Designation.Value} ({item.Name})"
                    : item.Name;
                missing.Add(label);
            }
            else
            {
                totalMass += item.UnitMassKg.Value * item.TotalQuantity;
            }
        }

        if (missing.Count > 0)
        {
            return MassCalculationResult.MissingMass(missing);
        }

        return MassCalculationResult.Success(totalMass);
    }

    public IReadOnlyList<ConsolidatedBomItem> BuildConsolidatedSpecification(IReadOnlyList<BomHierarchyNode> hierarchyNodes)
    {
        if (hierarchyNodes.Count == 0)
            return Array.Empty<ConsolidatedBomItem>();

        // Вычисляем накопленное количество для каждого узла по пути в дереве
        var cumulativeQuantities = CalculateCumulativeQuantities(hierarchyNodes);

        // Группируем листовые детали и стандартные изделия (Assembly не включаются в спецификацию деталей)
        var leafGroups = hierarchyNodes
            .Where(n => n.Type != ItemType.Assembly && n.Level > 0)
            .GroupBy(n => n.ObjectId);

        var result = new List<ConsolidatedBomItem>();

        foreach (var group in leafGroups)
        {
            var first = group.First();
            int totalQty = 0;

            foreach (var node in group)
            {
                if (cumulativeQuantities.TryGetValue(node, out var qty))
                    totalQty += qty;
                else
                    totalQty += node.Quantity;
            }

            result.Add(new ConsolidatedBomItem(
                first.ObjectId,
                first.Type,
                first.Designation,
                first.Name,
                totalQty,
                first.MassKg,
                first.Material));
        }

        return result
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Designation?.Value ?? x.Name)
            .ToList();
    }

    public async Task<MassCalculationResult> CalculateAssemblyMassAsync(Guid assemblyId, CancellationToken ct = default)
    {
        if (_queryRepository == null)
            throw new InvalidOperationException("Репозиторий запросов состава не настроен.");

        var nodes = await _queryRepository.GetFullHierarchyAsync(assemblyId, ct);
        return CalculateMass(nodes);
    }

    public async Task<IReadOnlyList<ConsolidatedBomItem>> GetConsolidatedSpecificationAsync(Guid assemblyId, CancellationToken ct = default)
    {
        if (_queryRepository == null)
            throw new InvalidOperationException("Репозиторий запросов состава не настроен.");

        var nodes = await _queryRepository.GetFullHierarchyAsync(assemblyId, ct);
        return BuildConsolidatedSpecification(nodes);
    }

    private static Dictionary<BomHierarchyNode, int> CalculateCumulativeQuantities(IReadOnlyList<BomHierarchyNode> nodes)
    {
        var result = new Dictionary<BomHierarchyNode, int>(ReferenceEqualityComparer.Instance);
        var root = nodes.FirstOrDefault(n => n.Level == 0);

        if (root == null)
        {
            // Если root не передан отдельно, считаем узлы Level 1 корнями
            foreach (var n in nodes)
                result[n] = n.Quantity;
            return result;
        }

        result[root] = 1;

        // Воспроизводим обход по уровням Level
        var maxLevel = nodes.Max(n => n.Level);
        for (int lvl = 1; lvl <= maxLevel; lvl++)
        {
            var currentLevelNodes = nodes.Where(n => n.Level == lvl).ToList();
            foreach (var node in currentLevelNodes)
            {
                // Ищем родительский узел на уровне lvl - 1
                var parent = nodes.FirstOrDefault(p => p.Level == lvl - 1 && p.ObjectId == node.ParentObjectId);
                int parentQty = parent != null && result.TryGetValue(parent, out var pQ) ? pQ : 1;
                result[node] = parentQty * node.Quantity;
            }
        }

        return result;
    }
}


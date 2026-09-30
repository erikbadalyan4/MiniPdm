using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Application.Models.Bom;

/// <summary>
/// Узел иерархии состава изделия, возвращаемый рекурсивным CTE-запросом.
/// </summary>
public sealed record BomHierarchyNode(
    Guid ObjectId,
    ItemType Type,
    Designation? Designation,
    string Name,
    Guid? CurrentVersionId,
    int? VersionNumber,
    VersionState? State,
    string? Material,
    decimal? MassKg,
    int Quantity,
    int Level,
    Guid? ParentObjectId,
    Guid? ParentVersionId,
    string HierarchyPath);

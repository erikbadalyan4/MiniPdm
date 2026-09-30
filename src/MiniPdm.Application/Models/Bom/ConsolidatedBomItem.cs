using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Application.Models.Bom;

/// <summary>
/// Строка сводной спецификации.
/// Плоский список всех деталей и стандартных изделий с суммарным количеством по дереву.
/// </summary>
public sealed record ConsolidatedBomItem(
    Guid ObjectId,
    ItemType Type,
    Designation? Designation,
    string Name,
    int TotalQuantity,
    decimal? UnitMassKg,
    string? Material)
{
    public decimal? TotalMassKg => UnitMassKg.HasValue ? UnitMassKg.Value * TotalQuantity : null;
}

using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Application.Models.Bom;

public enum BomDiffKind
{
    Added,
    Removed,
    QuantityChanged,
    Unchanged
}

public sealed record BomDiffItem(
    Guid ObjectId,
    ItemType Type,
    Designation? Designation,
    string Name,
    int? OldQuantity,
    int? NewQuantity,
    BomDiffKind Kind);

public sealed class BomDiffResult
{
    public IReadOnlyList<BomDiffItem> Changes { get; }
    public int AddedCount { get; }
    public int RemovedCount { get; }
    public int ModifiedCount { get; }

    public BomDiffResult(IReadOnlyList<BomDiffItem> changes)
    {
        Changes = changes;
        AddedCount = changes.Count(c => c.Kind == BomDiffKind.Added);
        RemovedCount = changes.Count(c => c.Kind == BomDiffKind.Removed);
        ModifiedCount = changes.Count(c => c.Kind == BomDiffKind.QuantityChanged);
    }
}

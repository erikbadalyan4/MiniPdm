using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Services;

public sealed class BomDiffService : IBomDiffService
{
    private readonly IItemRepository? _itemRepository;

    public BomDiffService(IItemRepository? itemRepository = null)
    {
        _itemRepository = itemRepository;
    }

    public BomDiffResult CompareVersions(
        IEnumerable<BomLink> oldLinks,
        IEnumerable<BomLink> newLinks,
        Func<Guid, Item?> itemResolver)
    {
        var oldDict = oldLinks.ToDictionary(l => l.ChildObjectId, l => l.Quantity);
        var newDict = newLinks.ToDictionary(l => l.ChildObjectId, l => l.Quantity);

        var allChildIds = oldDict.Keys.Union(newDict.Keys).ToList();
        var diffItems = new List<BomDiffItem>();

        foreach (var childId in allChildIds)
        {
            var item = itemResolver(childId);
            var itemType = item?.Type ?? ItemType.Part;
            var designation = item?.Designation;
            var name = item?.Name ?? childId.ToString();

            bool inOld = oldDict.TryGetValue(childId, out var oldQty);
            bool inNew = newDict.TryGetValue(childId, out var newQty);

            if (!inOld && inNew)
            {
                diffItems.Add(new BomDiffItem(childId, itemType, designation, name, null, newQty, BomDiffKind.Added));
            }
            else if (inOld && !inNew)
            {
                diffItems.Add(new BomDiffItem(childId, itemType, designation, name, oldQty, null, BomDiffKind.Removed));
            }
            else if (inOld && inNew)
            {
                if (oldQty != newQty)
                {
                    diffItems.Add(new BomDiffItem(childId, itemType, designation, name, oldQty, newQty, BomDiffKind.QuantityChanged));
                }
                else
                {
                    diffItems.Add(new BomDiffItem(childId, itemType, designation, name, oldQty, newQty, BomDiffKind.Unchanged));
                }
            }
        }

        var sorted = diffItems
            .OrderBy(d => d.Kind)
            .ThenBy(d => d.Type)
            .ThenBy(d => d.Designation?.Value ?? d.Name)
            .ToList();

        return new BomDiffResult(sorted);
    }

    public async Task<BomDiffResult> CompareVersionsAsync(
        Guid oldVersionId,
        Guid newVersionId,
        CancellationToken ct = default)
    {
        if (_itemRepository == null)
            throw new InvalidOperationException("Репозиторий объектов не настроен.");

        var oldLinks = await _itemRepository.GetBomLinksByVersionIdAsync(oldVersionId, ct);
        var newLinks = await _itemRepository.GetBomLinksByVersionIdAsync(newVersionId, ct);

        var itemCache = new Dictionary<Guid, Item?>();
        async Task<Item?> ResolveItemAsync(Guid id)
        {
            if (!itemCache.TryGetValue(id, out var item))
            {
                item = await _itemRepository.GetByIdAsync(id, ct);
                itemCache[id] = item;
            }
            return item;
        }

        var allIds = oldLinks.Select(l => l.ChildObjectId).Union(newLinks.Select(l => l.ChildObjectId)).Distinct();
        foreach (var id in allIds)
        {
            await ResolveItemAsync(id);
        }

        return CompareVersions(oldLinks, newLinks, id => itemCache.GetValueOrDefault(id));
    }
}


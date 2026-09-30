using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Entities;

namespace MiniPdm.Application.Services;

public interface IBomDiffService
{
    /// <summary>
    /// Сравнивает состав двух версий сборок в памяти.
    /// </summary>
    BomDiffResult CompareVersions(
        IEnumerable<BomLink> oldLinks,
        IEnumerable<BomLink> newLinks,
        Func<Guid, Item?> itemResolver);

    /// <summary>
    /// Сравнивает две версии сборки по их идентификаторам из репозитория.
    /// </summary>
    Task<BomDiffResult> CompareVersionsAsync(
        Guid oldVersionId,
        Guid newVersionId,
        CancellationToken ct = default);
}

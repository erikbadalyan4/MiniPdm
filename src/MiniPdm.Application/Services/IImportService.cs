using MiniPdm.Application.Models.Cad;
using MiniPdm.Application.Models.Import;

namespace MiniPdm.Application.Services;

public sealed record ImportProgress(int ProcessedCount, int TotalCount, string CurrentFileName);

public interface IImportService
{
    /// <summary>
    /// Импортирует документы из папки с использованием ICadDocumentReader.
    /// </summary>
    Task<ImportReport> ImportFolderAsync(
        string folderPath,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Выполняет бизнес-логику импорта набора уже прочитанных документов САПР.
    /// Позволяет тестировать логику без обращения к файловой системе.
    /// </summary>
    Task<ImportReport> ImportDocumentsAsync(
        IReadOnlyList<CadDocument> documents,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default);
}

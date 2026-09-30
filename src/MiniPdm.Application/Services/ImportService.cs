using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Cad;
using MiniPdm.Application.Models.Import;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Application.Services;

public sealed class ImportService : IImportService
{
    private readonly ICadDocumentReader _reader;
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportService(
        ICadDocumentReader reader,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork)
    {
        _reader = reader;
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportReport> ImportFolderAsync(
        string folderPath,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".a3d", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".m3d", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var readDocuments = new List<CadDocument>();
        var readErrors = new List<ImportItemResult>();

        for (int i = 0; i < files.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = files[i];
            var fileName = Path.GetFileName(file);
            progress?.Report(new ImportProgress(i + 1, files.Count, fileName));

            try
            {
                var doc = await _reader.ReadAsync(file, ct);
                readDocuments.Add(doc);
            }
            catch (Exception ex)
            {
                readErrors.Add(new ImportItemResult(fileName, ImportSeverity.Error, $"Ошибка чтения файла: {ex.Message}"));
            }
        }

        var report = await ImportDocumentsAsync(readDocuments, progress, ct);
        var combinedResults = readErrors.Concat(report.Items).ToList();
        return new ImportReport(combinedResults);
    }

    public async Task<ImportReport> ImportDocumentsAsync(
        IReadOnlyList<CadDocument> documents,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, ImportItemResult>(StringComparer.OrdinalIgnoreCase);
        var validDocs = new Dictionary<string, CadDocument>(StringComparer.OrdinalIgnoreCase);

        // 1. Предварительная валидация формата и обязательных полей
        foreach (var doc in documents)
        {
            var fileName = doc.FileName;

            if (string.IsNullOrWhiteSpace(doc.Name))
            {
                results[fileName] = new ImportItemResult(fileName, ImportSeverity.Error, "Не указано наименование объекта");
                continue;
            }

            if (!Enum.TryParse<ItemType>(doc.Type, true, out var itemType))
            {
                results[fileName] = new ImportItemResult(fileName, ImportSeverity.Error, $"Неизвестный тип объекта: '{doc.Type}'");
                continue;
            }

            if (itemType == ItemType.StandardPart)
            {
                if (!string.IsNullOrWhiteSpace(doc.Designation))
                {
                    results[fileName] = new ImportItemResult(fileName, ImportSeverity.Error, "Стандартное изделие не должно иметь обозначения");
                    continue;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(doc.Designation))
                {
                    results[fileName] = new ImportItemResult(fileName, ImportSeverity.Error, "Для детали или сборочной единицы обозначение обязательно");
                    continue;
                }

                if (!Designation.TryCreate(doc.Designation, out _, out var designationError))
                {
                    results[fileName] = new ImportItemResult(fileName, ImportSeverity.Error, designationError!);
                    continue;
                }
            }

            validDocs[fileName] = doc;
        }

        // 2. Проверка уникальности обозначений в пакете:
        // Если у нескольких файлов совпадает обозначение, отклоняются все такие файлы
        var designationGroups = validDocs.Values
            .Where(d => !string.IsNullOrWhiteSpace(d.Designation))
            .GroupBy(d => d.Designation!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in designationGroups)
        {
            var fileNames = string.Join(", ", group.Select(d => d.FileName));
            foreach (var doc in group)
            {
                results[doc.FileName] = new ImportItemResult(
                    doc.FileName,
                    ImportSeverity.Error,
                    $"Одинаковое обозначение '{group.Key}' найдено в нескольких файлах: {fileNames}");
                validDocs.Remove(doc.FileName);
            }
        }

        // 3. Проверка количества вхождения компонентов сборки (count > 0)
        var assembliesToCheck = validDocs.Values
            .Where(d => string.Equals(d.Type, nameof(ItemType.Assembly), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var assembly in assembliesToCheck)
        {
            var invalidComp = assembly.Components.FirstOrDefault(c => c.Count <= 0);
            if (invalidComp != null)
            {
                results[assembly.FileName] = new ImportItemResult(
                    assembly.FileName,
                    ImportSeverity.Error,
                    $"Количество компонента '{invalidComp.File}' меньше или равно нулю: {invalidComp.Count}");
                validDocs.Remove(assembly.FileName);
            }
        }

        // 4. Каскадное отклонение:
        // Сборка, ссылающаяся на отклоненный или отсутствующий объект, тоже отклоняется, и так далее вверх по дереву
        bool cascadeChanged;
        do
        {
            cascadeChanged = false;
            var remainingAssemblies = validDocs.Values
                .Where(d => string.Equals(d.Type, nameof(ItemType.Assembly), StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var assembly in remainingAssemblies)
            {
                foreach (var comp in assembly.Components)
                {
                    if (results.TryGetValue(comp.File, out var rejectedComp))
                    {
                        results[assembly.FileName] = new ImportItemResult(
                            assembly.FileName,
                            ImportSeverity.Error,
                            $"Компонент '{comp.File}' отклонён");
                        validDocs.Remove(assembly.FileName);
                        cascadeChanged = true;
                        break;
                    }

                    if (!validDocs.ContainsKey(comp.File))
                    {
                        results[assembly.FileName] = new ImportItemResult(
                            assembly.FileName,
                            ImportSeverity.Error,
                            $"Ссылка на отсутствующий файл '{comp.File}'");
                        validDocs.Remove(assembly.FileName);
                        cascadeChanged = true;
                        break;
                    }
                }
            }
        } while (cascadeChanged);

        // 5. Проверка предупреждений для принятых деталей без массы
        foreach (var doc in validDocs.Values)
        {
            if (string.Equals(doc.Type, nameof(ItemType.Part), StringComparison.OrdinalIgnoreCase))
            {
                if (!doc.Properties.Mass.HasValue)
                {
                    results[doc.FileName] = new ImportItemResult(doc.FileName, ImportSeverity.Warning, "Не указана масса");
                }
                else
                {
                    results[doc.FileName] = new ImportItemResult(doc.FileName, ImportSeverity.Success, "Успешно импортирован");
                }
            }
            else
            {
                results[doc.FileName] = new ImportItemResult(doc.FileName, ImportSeverity.Success, "Успешно импортирован");
            }
        }

        // 6. Персистентность в транзакции: создание объектов и версий
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Карта соответствия fileName -> Item для создания BOM-связей
            var fileToItemMap = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);

            // Сначала импортируем детали и стандартные изделия, затем сборки
            var sortedDocs = validDocs.Values
                .OrderBy(d => string.Equals(d.Type, nameof(ItemType.Assembly), StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ToList();

            foreach (var doc in sortedDocs)
            {
                ct.ThrowIfCancellationRequested();
                var itemType = Enum.Parse<ItemType>(doc.Type, true);
                var designation = !string.IsNullOrWhiteSpace(doc.Designation) ? Designation.Create(doc.Designation) : (Designation?)null;

                Item? existing = null;
                if (itemType == ItemType.StandardPart)
                    existing = await _itemRepository.GetByNameAsync(doc.Name, ct);
                else if (designation.HasValue)
                    existing = await _itemRepository.GetByDesignationAsync(designation.Value, ct);

                if (existing == null)
                {
                    // Новый объект
                    var newItem = itemType switch
                    {
                        ItemType.Assembly => Item.CreateAssembly(Guid.NewGuid(), designation!.Value, doc.Name),
                        ItemType.Part => Item.CreatePart(Guid.NewGuid(), designation!.Value, doc.Name),
                        ItemType.StandardPart => Item.CreateStandardPart(Guid.NewGuid(), doc.Name),
                        _ => throw new InvalidOperationException()
                    };

                    var version = newItem.CreateInitialVersion(doc.Properties.Material, doc.Properties.Mass);
                    await _itemRepository.AddAsync(newItem, ct);
                    await _itemRepository.SaveVersionAsync(version, ct);
                    await _itemRepository.UpdateAsync(newItem, ct);

                    fileToItemMap[doc.FileName] = newItem;
                }
                else
                {
                    // Повторный импорт
                    fileToItemMap[doc.FileName] = existing;
                    var currentVersion = existing.CurrentVersionId.HasValue
                        ? await _itemRepository.GetVersionByIdAsync(existing.CurrentVersionId.Value, ct)
                        : null;

                    bool propertiesChanged = currentVersion == null ||
                                             currentVersion.Material != doc.Properties.Material ||
                                             currentVersion.MassKg != doc.Properties.Mass;

                    if (propertiesChanged)
                    {
                        if (currentVersion == null || currentVersion.State == VersionState.InWork)
                        {
                            currentVersion?.UpdateProperties(doc.Properties.Material, doc.Properties.Mass);
                            if (currentVersion != null)
                                await _itemRepository.SaveVersionAsync(currentVersion, ct);
                        }
                        else if (currentVersion.State == VersionState.Approved)
                        {
                            var newVersion = existing.CreateNextVersion(doc.Properties.Material, doc.Properties.Mass);
                            await _itemRepository.SaveVersionAsync(newVersion, ct);
                            await _itemRepository.UpdateAsync(existing, ct);
                        }
                    }
                }
            }

            // Создание и обновление BOM-связей для сборок
            var assemblyDocs = validDocs.Values
                .Where(d => string.Equals(d.Type, nameof(ItemType.Assembly), StringComparison.OrdinalIgnoreCase));

            foreach (var doc in assemblyDocs)
            {
                ct.ThrowIfCancellationRequested();
                var assemblyItem = fileToItemMap[doc.FileName];
                var versionId = assemblyItem.CurrentVersionId!.Value;

                var links = new List<BomLink>();
                foreach (var comp in doc.Components)
                {
                    if (fileToItemMap.TryGetValue(comp.File, out var childItem))
                    {
                        links.Add(new BomLink(Guid.NewGuid(), versionId, childItem.Id, comp.Count));
                    }
                }

                await _itemRepository.SaveBomLinksAsync(versionId, links, ct);
            }

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        // Упорядочиваем результаты по имени файла для стабильного отчета
        var orderedResults = results.Values
            .OrderBy(r => r.Severity == ImportSeverity.Error ? 0 : r.Severity == ImportSeverity.Warning ? 1 : 2)
            .ThenBy(r => r.FileName)
            .ToList();

        return new ImportReport(orderedResults);
    }
}


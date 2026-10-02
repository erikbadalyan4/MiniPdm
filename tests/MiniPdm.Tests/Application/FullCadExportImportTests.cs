using FluentAssertions;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Import;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.ValueObjects;
using MiniPdm.Infrastructure.Cad;

namespace MiniPdm.Tests.Application;

public class FullCadExportImportTests
{
    [Fact]
    public async Task ImportFolder_CadExport_ShouldProduceExpectedReport()
    {
        var basePath = AppContext.BaseDirectory;
        var folder = Path.GetFullPath(Path.Combine(basePath, "../../../../cad-export"));

        if (!Directory.Exists(folder))
            return;

        var repo = new MemoryItemRepository();
        var uow = new MemoryUnitOfWork();
        var reader = new JsonCadDocumentReader();
        var importer = new ImportService(reader, repo, uow);

        var report = await importer.ImportFolderAsync(folder);

        report.Items.Should().NotBeEmpty();

        // 1. Проверяем, что синтаксическая ошибка в Отдушина.m3d поймана
        var otduhsina = report.Items.FirstOrDefault(i => i.FileName == "Отдушина.m3d");
        otduhsina.Should().NotBeNull();
        otduhsina!.Severity.Should().Be(ImportSeverity.Error);
        otduhsina.Reason.Should().Contain("Файл повреждён").And.Contain("JSON");

        // 2. Деталь без массы (Крышка смотровая.m3d) получила предупреждение
        var kryshka = report.Items.FirstOrDefault(i => i.FileName == "Крышка смотровая.m3d");
        kryshka.Should().NotBeNull();
        kryshka!.Severity.Should().Be(ImportSeverity.Warning);
    }

    [Fact]
    public async Task RepeatImport_CadExportV2_AfterApproval_ShouldCreateVersion2ForChangedItemsOnly()
    {
        var basePath = AppContext.BaseDirectory;
        var folder1 = Path.GetFullPath(Path.Combine(basePath, "../../../../cad-export"));
        var folder2 = Path.GetFullPath(Path.Combine(basePath, "../../../../cad-export-v2"));

        if (!Directory.Exists(folder1) || !Directory.Exists(folder2))
            return;

        var repo = new MemoryItemRepository();
        var uow = new MemoryUnitOfWork();
        var reader = new JsonCadDocumentReader();
        var importer = new ImportService(reader, repo, uow);

        // 1. Первый импорт
        var report1 = await importer.ImportFolderAsync(folder1);
        report1.AcceptedCount.Should().Be(38);

        // 2. Утверждаем все принятые версии
        foreach (var v in repo.Versions.ToList())
        {
            v.Approve();
        }

        // 3. Повторный импорт v2
        var report2 = await importer.ImportFolderAsync(folder2);
        report2.Items.Should().NotBeEmpty();

        // Колесо зубчатое: создана версия 2 с новой массой 5.92 кг
        var gear = repo.Items.FirstOrDefault(i => i.Name == "Колесо зубчатое");
        gear.Should().NotBeNull();
        gear!.Versions.Should().HaveCount(2);

        // Крышка подшипника в сборе: создана версия 2
        var coverAssy = repo.Items.FirstOrDefault(i => i.Name == "Крышка подшипника в сборе");
        coverAssy.Should().NotBeNull();
        coverAssy!.Versions.Should().HaveCount(2);

        // Проверяем связи версии 1 (3 прокладки) и версии 2 (4 прокладки)
        var v1 = coverAssy.Versions.First(v => v.VersionNumber == 1);
        var v2 = coverAssy.Versions.First(v => v.VersionNumber == 2);

        var linksV1 = await repo.GetBomLinksByVersionIdAsync(v1.Id);
        var linksV2 = await repo.GetBomLinksByVersionIdAsync(v2.Id);

        var gasket = repo.Items.First(i => i.Name == "Прокладка регулировочная");
        linksV1.First(l => l.ChildObjectId == gasket.Id).Quantity.Should().Be(3);
        linksV2.First(l => l.ChildObjectId == gasket.Id).Quantity.Should().Be(4);
    }

    private class MemoryItemRepository : IItemRepository
    {
        public readonly List<Item> Items = new();
        public readonly List<ItemVersion> Versions = new();
        public readonly List<BomLink> Links = new();

        public Task AddAsync(Item item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Item>>(Items);

        public Task<Item?> GetByDesignationAsync(Designation designation, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Designation == designation));

        public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<Item?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Name == name));

        public Task<ItemVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct = default) =>
            Task.FromResult(Versions.FirstOrDefault(v => v.Id == versionId));

        public Task<IReadOnlyList<BomLink>> GetBomLinksByVersionIdAsync(Guid versionId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BomLink>>(Links.Where(l => l.ParentVersionId == versionId).ToList());

        public Task SaveBomLinksAsync(Guid parentVersionId, IEnumerable<BomLink> links, CancellationToken ct = default)
        {
            Links.RemoveAll(l => l.ParentVersionId == parentVersionId);
            Links.AddRange(links);
            return Task.CompletedTask;
        }

        public Task SaveVersionAsync(ItemVersion version, CancellationToken ct = default)
        {
            Versions.RemoveAll(v => v.Id == version.Id);
            Versions.Add(version);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Item>> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Item>>(Items.Where(i => i.Name.Contains(query)).ToList());

        public Task UpdateAsync(Item item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class MemoryUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}


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

        // 2. Деталь без массы (Крышка смотровая.m3d) получила предупреждение
        var kryshka = report.Items.FirstOrDefault(i => i.FileName == "Крышка смотровая.m3d");
        kryshka.Should().NotBeNull();
        kryshka!.Severity.Should().Be(ImportSeverity.Warning);
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


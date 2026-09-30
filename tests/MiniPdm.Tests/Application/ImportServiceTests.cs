using FluentAssertions;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Cad;
using MiniPdm.Application.Models.Import;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Tests.Application;

public class ImportServiceTests
{
    private readonly FakeItemRepository _itemRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCadReader _reader = new();
    private readonly ImportService _importService;

    public ImportServiceTests()
    {
        _importService = new ImportService(_reader, _itemRepo, _unitOfWork);
    }

    [Fact]
    public async Task Import_WhenDuplicateDesignationsInBatch_ShouldRejectAllSuchFiles()
    {
        var doc1 = new CadDocument(1, "Деталь1.m3d", "Part", "АБВГ.111111.001", "Деталь 1", new(null, 1.0m), Array.Empty<CadComponent>());
        var doc2 = new CadDocument(1, "Деталь2.m3d", "Part", "АБВГ.111111.001", "Деталь 2", new(null, 2.0m), Array.Empty<CadComponent>());

        var report = await _importService.ImportDocumentsAsync(new[] { doc1, doc2 });

        report.RejectedCount.Should().Be(2);
        report.AcceptedCount.Should().Be(0);
        report.Items.Should().AllSatisfy(item => item.Severity.Should().Be(ImportSeverity.Error));
    }

    [Fact]
    public async Task Import_WhenComponentCountIsZeroOrNegative_ShouldRejectAssembly()
    {
        var partDoc = new CadDocument(1, "Вал.m3d", "Part", "АБВГ.111111.001", "Вал", new("Сталь", 5.0m), Array.Empty<CadComponent>());
        var assemblyDoc = new CadDocument(1, "Сборка.a3d", "Assembly", "АБВГ.222222.001", "Сборка", new(null, null),
            new List<CadComponent> { new("Вал.m3d", 0) });

        var report = await _importService.ImportDocumentsAsync(new[] { partDoc, assemblyDoc });

        report.Items.First(x => x.FileName == "Вал.m3d").Severity.Should().Be(ImportSeverity.Success);
        var assemblyResult = report.Items.First(x => x.FileName == "Сборка.a3d");
        assemblyResult.Severity.Should().Be(ImportSeverity.Error);
        assemblyResult.Reason.Should().Contain("меньше или равно нулю");
    }

    [Fact]
    public async Task Import_WhenComponentMissingOrRejected_ShouldCascadeRejectParentAssemblies()
    {
        var leafPart = new CadDocument(1, "Лист.m3d", "Part", "АБВГ.111111.001", "Лист", new("Сталь", 1.0m), Array.Empty<CadComponent>());
        
        // SubAssembly ссылается на несуществующий файл
        var subAssembly = new CadDocument(1, "Подсборка.a3d", "Assembly", "АБВГ.222222.001", "Подсборка", new(null, null),
            new List<CadComponent> { new("Несуществующий.m3d", 1) });

        // RootAssembly ссылается на SubAssembly и Лист
        var rootAssembly = new CadDocument(1, "Главная.a3d", "Assembly", "АБВГ.333333.001", "Главная", new(null, null),
            new List<CadComponent> { new("Подсборка.a3d", 1), new("Лист.m3d", 2) });

        var report = await _importService.ImportDocumentsAsync(new[] { leafPart, subAssembly, rootAssembly });

        report.Items.First(x => x.FileName == "Лист.m3d").Severity.Should().Be(ImportSeverity.Success);
        
        var subRes = report.Items.First(x => x.FileName == "Подсборка.a3d");
        subRes.Severity.Should().Be(ImportSeverity.Error);
        subRes.Reason.Should().Contain("Ссылка на отсутствующий файл");

        var rootRes = report.Items.First(x => x.FileName == "Главная.a3d");
        rootRes.Severity.Should().Be(ImportSeverity.Error);
        rootRes.Reason.Should().Contain("отклонён");
    }

    [Fact]
    public async Task Import_WhenPartHasNoMass_ShouldImportWithWarning()
    {
        var partDoc = new CadDocument(1, "Крышка.m3d", "Part", "АБВГ.111111.001", "Крышка", new("Сталь", null), Array.Empty<CadComponent>());

        var report = await _importService.ImportDocumentsAsync(new[] { partDoc });

        report.AcceptedCount.Should().Be(1);
        report.WarningCount.Should().Be(1);
        report.RejectedCount.Should().Be(0);

        var result = report.Items.First(x => x.FileName == "Крышка.m3d");
        result.Severity.Should().Be(ImportSeverity.Warning);
        result.Reason.Should().Be("Не указана масса");
    }

    private class FakeItemRepository : IItemRepository
    {
        private readonly List<Item> _items = new();
        private readonly List<ItemVersion> _versions = new();
        private readonly List<BomLink> _links = new();

        public Task AddAsync(Item item, CancellationToken ct = default)
        {
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Item>>(_items);

        public Task<Item?> GetByDesignationAsync(Designation designation, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(i => i.Designation == designation));

        public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

        public Task<Item?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(i => i.Name == name));

        public Task<ItemVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct = default) =>
            Task.FromResult(_versions.FirstOrDefault(v => v.Id == versionId));

        public Task<IReadOnlyList<BomLink>> GetBomLinksByVersionIdAsync(Guid versionId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BomLink>>(_links.Where(l => l.ParentVersionId == versionId).ToList());

        public Task SaveBomLinksAsync(Guid parentVersionId, IEnumerable<BomLink> links, CancellationToken ct = default)
        {
            _links.RemoveAll(l => l.ParentVersionId == parentVersionId);
            _links.AddRange(links);
            return Task.CompletedTask;
        }

        public Task SaveVersionAsync(ItemVersion version, CancellationToken ct = default)
        {
            _versions.RemoveAll(v => v.Id == version.Id);
            _versions.Add(version);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Item>> SearchAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Item>>(_items.Where(i => i.Name.Contains(query)).ToList());

        public Task UpdateAsync(Item item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeCadReader : ICadDocumentReader
    {
        public Task<CadDocument> ReadAsync(string path, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}


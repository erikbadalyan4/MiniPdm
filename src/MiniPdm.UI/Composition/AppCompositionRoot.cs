using Jab;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Services;
using MiniPdm.Infrastructure.Cad;
using MiniPdm.Infrastructure.Data;
using MiniPdm.Infrastructure.Export;
using MiniPdm.Infrastructure.Repositories;
using MiniPdm.UI.ViewModels;

namespace MiniPdm.UI.Composition;

[ServiceProvider]
[Singleton<IDbConnectionFactory>(Factory = nameof(GetConnectionFactory))]
[Singleton<DbUnitOfWork>]
[Singleton<IUnitOfWork>(Factory = nameof(GetUnitOfWork))]
[Singleton<IDbContext>(Factory = nameof(GetDbContext))]
[Singleton<ICadDocumentReader, JsonCadDocumentReader>]
[Singleton<ISpecificationExporter, CsvSpecificationExporter>]
[Singleton<ICycleDetector, CycleDetector>]
[Singleton<IBomCalculationService, BomCalculationService>]
[Singleton<IBomDiffService, BomDiffService>]
[Singleton<IImportService, ImportService>]
[Singleton<IItemRepository, DapperItemRepository>]
[Singleton<IBomQueryRepository, DapperBomQueryRepository>]
[Singleton<MainViewModel>]
[Singleton<MainWindow>]
public partial class AppCompositionRoot
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AppCompositionRoot(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbConnectionFactory GetConnectionFactory() => _connectionFactory;
    public IUnitOfWork GetUnitOfWork() => GetService<DbUnitOfWork>();
    public IDbContext GetDbContext() => GetService<DbUnitOfWork>();
}

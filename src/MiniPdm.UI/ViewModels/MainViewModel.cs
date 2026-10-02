using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Application.Models.Import;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Export;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IItemRepository _itemRepository;
    private readonly IBomQueryRepository _queryRepository;
    private readonly IImportService _importService;
    private readonly IBomCalculationService _calculationService;
    private readonly IBomDiffService _diffService;
    private readonly ISpecificationExporter _exporter;

    private string _searchQuery = string.Empty;
    private BomTreeItemViewModel? _selectedTreeItem;
    private bool _isImporting;
    private int _importProgress;
    private string _importStatusText = string.Empty;
    private CancellationTokenSource? _importCts;

    public ObservableCollection<BomTreeItemViewModel> RootItems { get; } = new();
    public ObservableCollection<FirstLevelComponentViewModel> FirstLevelComponents { get; } = new();
    public ItemDetailsViewModel Details { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadRootItemsAsync();
            }
        }
    }

    public BomTreeItemViewModel? SelectedTreeItem
    {
        get => _selectedTreeItem;
        set
        {
            if (SetProperty(ref _selectedTreeItem, value))
            {
                _ = OnTreeItemSelectedAsync(value);
            }
        }
    }

    public bool IsImporting
    {
        get => _isImporting;
        private set
        {
            if (SetProperty(ref _isImporting, value))
            {
                (ImportFolderCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CancelImportCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int ImportProgress
    {
        get => _importProgress;
        set => SetProperty(ref _importProgress, value);
    }

    public string ImportStatusText
    {
        get => _importStatusText;
        set => SetProperty(ref _importStatusText, value);
    }

    public bool CanCalculateMass => SelectedTreeItem != null && SelectedTreeItem.Type == ItemType.Assembly;
    public bool CanOpenSpecification => SelectedTreeItem != null && SelectedTreeItem.Type == ItemType.Assembly;
    public bool CanOpenDiff => SelectedTreeItem != null && SelectedTreeItem.Type == ItemType.Assembly && Details.Item?.Versions.Count > 1;

    public ICommand RefreshCommand { get; }
    public ICommand ImportFolderCommand { get; }
    public ICommand CancelImportCommand { get; }
    public ICommand CalculateMassCommand { get; }
    public ICommand OpenSpecificationCommand { get; }
    public ICommand OpenDiffCommand { get; }

    // Делегаты для отображения окон из View (без смешивания с code-behind)
    public Action<ImportReport, string>? ShowImportReportAction { get; set; }
    public Action<Guid, string>? ShowSpecificationAction { get; set; }
    public Action<Item>? ShowDiffAction { get; set; }
    public Func<string?>? SelectFolderAction { get; set; }

    public MainViewModel(
        IItemRepository itemRepository,
        IBomQueryRepository queryRepository,
        IImportService importService,
        IBomCalculationService calculationService,
        IBomDiffService diffService,
        ISpecificationExporter exporter)
    {
        _itemRepository = itemRepository;
        _queryRepository = queryRepository;
        _importService = importService;
        _calculationService = calculationService;
        _diffService = diffService;
        _exporter = exporter;

        Details = new ItemDetailsViewModel(_itemRepository, () => _ = RefreshCurrentItemAsync());

        RefreshCommand = new AsyncRelayCommand(LoadRootItemsAsync);
        ImportFolderCommand = new AsyncRelayCommand(ImportFolderAsync, () => !IsImporting);
        CancelImportCommand = new RelayCommand(CancelImport, () => IsImporting);
        CalculateMassCommand = new AsyncRelayCommand(CalculateMassAsync, () => CanCalculateMass);
        OpenSpecificationCommand = new RelayCommand(OpenSpecification, () => CanOpenSpecification);
        OpenDiffCommand = new RelayCommand(OpenDiff, () => CanOpenDiff);
    }

    public async Task InitializeAsync()
    {
        await LoadRootItemsAsync();
    }

    public async Task LoadRootItemsAsync()
    {
        try
        {
            IReadOnlyList<Item> items;
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                items = await _itemRepository.GetAllAsync();
            }
            else
            {
                items = await _itemRepository.SearchAsync(_searchQuery.Trim());
            }

            // Показываем в дереве верхнего уровня сборочные единицы или результаты поиска
            var displayItems = string.IsNullOrWhiteSpace(_searchQuery)
                ? items.Where(i => i.Type == ItemType.Assembly).ToList()
                : items.ToList();

            RootItems.Clear();
            foreach (var item in displayItems)
            {
                var vm = new BomTreeItemViewModel(
                    item.Id,
                    item.Type,
                    item.Designation,
                    item.Name,
                    item.GetCurrentVersion()?.Id,
                    quantity: 1,
                    _queryRepository,
                    isRoot: true);

                RootItems.Add(vm);
            }

            if (RootItems.Count > 0 && SelectedTreeItem == null)
            {
                SelectedTreeItem = RootItems[0];
            }
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Ошибка загрузки объектов: {ex.Message}";
        }
    }

    private async Task CalculateMassAsync()
    {
        if (SelectedTreeItem == null || SelectedTreeItem.Type != ItemType.Assembly)
            return;

        try
        {
            var result = await _calculationService.CalculateAssemblyMassAsync(SelectedTreeItem.ObjectId);
            if (result.IsSuccess && result.TotalMassKg.HasValue)
            {
                Details.SetCalculatedMass(result.TotalMassKg.Value);
            }
            else
            {
                Details.SetMassMessage(result.ErrorMessage ?? "Отсутствует масса у компонентов");
            }
        }
        catch (Exception ex)
        {
            Details.SetMassMessage($"Ошибка расчёта: {ex.Message}");
        }
    }

    private async Task OnTreeItemSelectedAsync(BomTreeItemViewModel? treeItem)
    {
        OnPropertyChanged(nameof(CanCalculateMass));
        OnPropertyChanged(nameof(CanOpenSpecification));
        OnPropertyChanged(nameof(CanOpenDiff));
        (CalculateMassCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenSpecificationCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (OpenDiffCommand as RelayCommand)?.RaiseCanExecuteChanged();

        if (treeItem == null)
        {
            Details.SetItem(null, null);
            FirstLevelComponents.Clear();
            return;
        }

        var item = await _itemRepository.GetByIdAsync(treeItem.ObjectId);
        var version = item?.GetCurrentVersion();
        Details.SetItem(item, version);

        FirstLevelComponents.Clear();
        if (version != null && item?.Type == ItemType.Assembly)
        {
            var components = await _queryRepository.GetFirstLevelComponentsAsync(version.Id);
            foreach (var comp in components)
            {
                var desig = comp.Designation.HasValue ? comp.Designation.Value.Value : "—";
                var mass = comp.MassKg.HasValue ? $"{comp.MassKg.Value:0.##}".Replace('.', ',') : "—";
                FirstLevelComponents.Add(new FirstLevelComponentViewModel(desig, comp.Name, comp.Quantity, mass));
            }
        }

        OnPropertyChanged(nameof(CanOpenDiff));
    }

    private async Task RefreshCurrentItemAsync()
    {
        if (SelectedTreeItem != null)
        {
            await OnTreeItemSelectedAsync(SelectedTreeItem);
        }
        await LoadRootItemsAsync();
    }

    private async Task ImportFolderAsync()
    {
        var folder = SelectFolderAction?.Invoke();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        IsImporting = true;
        ImportProgress = 0;
        ImportStatusText = "Запуск импорта...";
        _importCts = new CancellationTokenSource();

        Serilog.Log.Information("Старт импорта файлов из папки {FolderPath}", folder);

        var progressHandler = new Progress<ImportProgress>(p =>
        {
            ImportProgress = p.TotalCount > 0 ? (int)((double)p.ProcessedCount / p.TotalCount * 100) : 0;
            ImportStatusText = $"Обработка: {p.CurrentFileName} ({p.ProcessedCount}/{p.TotalCount})";
        });

        try
        {
            var report = await _importService.ImportFolderAsync(folder, progressHandler, _importCts.Token);
            ImportStatusText = $"Импорт завершен: принято {report.AcceptedCount}, ошибок {report.RejectedCount}";
            Serilog.Log.Information("Импорт из папки {FolderPath} успешно завершен. Принято: {AcceptedCount}, Ошибок: {RejectedCount}", folder, report.AcceptedCount, report.RejectedCount);
            await LoadRootItemsAsync();
            ShowImportReportAction?.Invoke(report, folder);
        }
        catch (OperationCanceledException)
        {
            ImportStatusText = "Импорт отменен пользователем.";
            Serilog.Log.Warning("Импорт из папки {FolderPath} был отменен пользователем", folder);
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Ошибка при импорте: {ex.Message}";
            Serilog.Log.Error(ex, "Ошибка при выполнении импорта из папки {FolderPath}", folder);
        }
        finally
        {
            IsImporting = false;
            _importCts?.Dispose();
            _importCts = null;
        }
    }

    private void CancelImport()
    {
        Serilog.Log.Information("Запрошена отмена операции импорта");
        _importCts?.Cancel();
    }

    private void OpenSpecification()
    {
        if (SelectedTreeItem != null)
        {
            ShowSpecificationAction?.Invoke(SelectedTreeItem.ObjectId, SelectedTreeItem.DisplayText);
        }
    }

    private void OpenDiff()
    {
        if (Details.Item != null)
        {
            ShowDiffAction?.Invoke(Details.Item);
        }
    }
}

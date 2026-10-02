using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Export;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class ConsolidatedSpecificationRowViewModel
{
    public string TypeDisplay { get; }
    public string DesignationDisplay { get; }
    public string Name { get; }
    public string MaterialDisplay { get; }
    public int Quantity { get; }
    public string UnitMassDisplay { get; }
    public string TotalMassDisplay { get; }

    public ConsolidatedSpecificationRowViewModel(ConsolidatedBomItem item)
    {
        TypeDisplay = item.Type switch
        {
            ItemType.Part => "Деталь",
            ItemType.StandardPart => "Стандартное изделие",
            ItemType.Assembly => "Сборка",
            _ => item.Type.ToString()
        };
        DesignationDisplay = item.Designation.HasValue ? item.Designation.Value.Value : "—";
        Name = item.Name;
        MaterialDisplay = string.IsNullOrWhiteSpace(item.Material) ? "—" : item.Material;
        Quantity = item.TotalQuantity;
        UnitMassDisplay = item.UnitMassKg.HasValue ? $"{item.UnitMassKg.Value:0.##}".Replace('.', ',') : "—";
        TotalMassDisplay = item.TotalMassKg.HasValue ? $"{item.TotalMassKg.Value:0.##}".Replace('.', ',') : "—";
    }
}

public sealed class ConsolidatedSpecificationViewModel : ViewModelBase
{
    private readonly IBomCalculationService _calculationService;
    private readonly ISpecificationExporter _exporter;
    private readonly Guid _assemblyId;

    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _hasMissingMass;

    public string AssemblyTitle { get; }
    public ObservableCollection<ConsolidatedBomItem> Items { get; } = new();
    public ObservableCollection<ConsolidatedSpecificationRowViewModel> DisplayItems { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasMissingMass
    {
        get => _hasMissingMass;
        private set => SetProperty(ref _hasMissingMass, value);
    }

    public ICommand ExportCommand { get; }

    public ConsolidatedSpecificationViewModel(
        Guid assemblyId,
        string assemblyTitle,
        IBomCalculationService calculationService,
        ISpecificationExporter exporter)
    {
        _assemblyId = assemblyId;
        AssemblyTitle = assemblyTitle;
        _calculationService = calculationService;
        _exporter = exporter;

        ExportCommand = new AsyncRelayCommand(ExportAsync, () => Items.Count > 0);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Выполняется рекурсивный расчёт состава...";

        try
        {
            var items = await _calculationService.GetConsolidatedSpecificationAsync(_assemblyId);
            Items.Clear();
            DisplayItems.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
                DisplayItems.Add(new ConsolidatedSpecificationRowViewModel(item));
            }

            var massResult = await _calculationService.CalculateAssemblyMassAsync(_assemblyId);
            if (massResult.IsSuccess && massResult.TotalMassKg.HasValue)
            {
                HasMissingMass = false;
                var massStr = $"{massResult.TotalMassKg.Value:0.##}".Replace('.', ',');
                StatusMessage = $"Расчётная масса сборки: {massStr} кг (позиций: {Items.Count})";
            }
            else
            {
                HasMissingMass = true;
                StatusMessage = massResult.ErrorMessage ?? "Отсутствует масса у части компонентов";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка расчёта: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportAsync()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = $"Спецификация_{AssemblyTitle.Replace(' ', '_')}.csv"
        };

        if (sfd.ShowDialog() == true)
        {
            await _exporter.ExportToCsvAsync(Items, sfd.FileName);
        }
    }
}


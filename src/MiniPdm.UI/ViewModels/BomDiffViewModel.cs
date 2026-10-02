using System.Collections.ObjectModel;
using System.Windows.Input;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class BomDiffRowViewModel
{
    public string KindDisplay { get; }
    public string KindColor { get; }
    public string TypeDisplay { get; }
    public string DesignationDisplay { get; }
    public string Name { get; }
    public int? OldQuantity { get; }
    public int? NewQuantity { get; }

    public BomDiffRowViewModel(BomDiffItem item)
    {
        KindDisplay = item.Kind switch
        {
            BomDiffKind.Added => "Добавлено",
            BomDiffKind.Removed => "Удалено",
            BomDiffKind.QuantityChanged => "Изменено кол-во",
            _ => item.Kind.ToString()
        };

        KindColor = item.Kind switch
        {
            BomDiffKind.Added => "#16A34A",
            BomDiffKind.Removed => "#DC2626",
            BomDiffKind.QuantityChanged => "#2563EB",
            _ => "#0F172A"
        };

        TypeDisplay = item.Type switch
        {
            ItemType.Part => "Деталь",
            ItemType.StandardPart => "Стандартное изделие",
            ItemType.Assembly => "Сборка",
            _ => item.Type.ToString()
        };

        DesignationDisplay = item.Designation.HasValue ? item.Designation.Value.Value : "—";
        Name = item.Name;
        OldQuantity = item.OldQuantity;
        NewQuantity = item.NewQuantity;
    }
}

public sealed class BomDiffViewModel : ViewModelBase
{
    private readonly IBomDiffService _diffService;
    private readonly Item _assembly;

    private ItemVersion? _selectedVersion1;
    private ItemVersion? _selectedVersion2;
    private string _summaryText = string.Empty;

    public string AssemblyName => $"{_assembly.Designation?.Value} {_assembly.Name}";
    public IReadOnlyCollection<ItemVersion> AvailableVersions => _assembly.Versions;

    public ItemVersion? SelectedVersion1
    {
        get => _selectedVersion1;
        set
        {
            if (SetProperty(ref _selectedVersion1, value))
                _ = CompareAsync();
        }
    }

    public ItemVersion? SelectedVersion2
    {
        get => _selectedVersion2;
        set
        {
            if (SetProperty(ref _selectedVersion2, value))
                _ = CompareAsync();
        }
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public ObservableCollection<BomDiffItem> Changes { get; } = new();
    public ObservableCollection<BomDiffRowViewModel> DisplayChanges { get; } = new();

    public BomDiffViewModel(Item assembly, IBomDiffService diffService)
    {
        _assembly = assembly;
        _diffService = diffService;

        var versions = assembly.Versions.OrderBy(v => v.VersionNumber).ToList();
        if (versions.Count >= 2)
        {
            _selectedVersion1 = versions[0];
            _selectedVersion2 = versions[^1];
        }
        else if (versions.Count == 1)
        {
            _selectedVersion1 = versions[0];
            _selectedVersion2 = versions[0];
        }

        _ = CompareAsync();
    }

    private async Task CompareAsync()
    {
        if (_selectedVersion1 == null || _selectedVersion2 == null)
            return;

        try
        {
            var diff = await _diffService.CompareVersionsAsync(_selectedVersion1.Id, _selectedVersion2.Id);

            Changes.Clear();
            DisplayChanges.Clear();
            foreach (var change in diff.Changes)
            {
                Changes.Add(change);
                DisplayChanges.Add(new BomDiffRowViewModel(change));
            }

            SummaryText = $"Добавлено: {diff.AddedCount} | Удалено: {diff.RemovedCount} | Изменено количество: {diff.ModifiedCount}";
        }
        catch (Exception ex)
        {
            SummaryText = $"Ошибка сравнения: {ex.Message}";
        }
    }
}

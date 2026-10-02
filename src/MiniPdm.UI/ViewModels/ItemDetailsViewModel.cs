using System.Windows.Input;
using MiniPdm.Application.Abstractions;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class ItemDetailsViewModel : ViewModelBase
{
    private readonly IItemRepository _itemRepository;
    private readonly Action _onItemChanged;

    private Item? _item;
    private ItemVersion? _currentVersion;
    private string? _massOverrideText;

    public Item? Item => _item;
    public ItemVersion? CurrentVersion => _currentVersion;

    public string Designation => _item?.Designation?.Value ?? "—";
    public string Name => _item?.Name ?? "—";
    public string TypeName => _item?.Type switch
    {
        ItemType.Assembly => "Сборка",
        ItemType.Part => "Деталь",
        ItemType.StandardPart => "Стандартное изделие",
        _ => "—"
    };

    public string Subtitle
    {
        get
        {
            if (_item == null) return string.Empty;
            if (_item.Type == ItemType.StandardPart) return "Стандартное изделие";
            return $"{Designation} · {TypeName}";
        }
    }

    public string VersionNumber => _currentVersion != null ? _currentVersion.VersionNumber.ToString() : "—";
    public string StateName => _currentVersion?.State switch
    {
        VersionState.InWork => "В работе",
        VersionState.Approved => "Утверждено",
        VersionState.Obsolete => "Аннулировано",
        _ => "—"
    };

    public string Material => _currentVersion?.Material ?? "—";
    public string MassKg => _currentVersion?.MassKg.HasValue == true
        ? $"{_currentVersion.MassKg.Value:0.##}".Replace('.', ',')
        : (_item?.Type == ItemType.Assembly ? "— (нажмите «Рассчитать массу»)" : "—");

    public string MassDisplay => _massOverrideText ?? MassKg;

    public void SetCalculatedMass(decimal mass)
    {
        _massOverrideText = $"{mass:0.##}".Replace('.', ',');
        OnPropertyChanged(nameof(MassDisplay));
    }

    public void SetMassMessage(string message)
    {
        _massOverrideText = message;
        OnPropertyChanged(nameof(MassDisplay));
    }

    public void ResetCalculatedMass()
    {
        _massOverrideText = null;
        OnPropertyChanged(nameof(MassDisplay));
    }

    public bool CanApprove => _currentVersion?.State == VersionState.InWork;
    public bool CanObsolete => _currentVersion?.State == VersionState.InWork || _currentVersion?.State == VersionState.Approved;
    public bool CanCreateNewVersion => _currentVersion?.State == VersionState.Approved;

    public ICommand ApproveCommand { get; }
    public ICommand ObsoleteCommand { get; }
    public ICommand CreateNewVersionCommand { get; }

    public ItemDetailsViewModel(IItemRepository itemRepository, Action onItemChanged)
    {
        _itemRepository = itemRepository;
        _onItemChanged = onItemChanged;

        ApproveCommand = new AsyncRelayCommand(ApproveAsync, () => CanApprove);
        ObsoleteCommand = new AsyncRelayCommand(ObsoleteAsync, () => CanObsolete);
        CreateNewVersionCommand = new AsyncRelayCommand(CreateNewVersionAsync, () => CanCreateNewVersion);
    }

    public void SetItem(Item? item, ItemVersion? currentVersion)
    {
        _item = item;
        _currentVersion = currentVersion;
        _massOverrideText = null;

        OnPropertyChanged(nameof(Item));
        OnPropertyChanged(nameof(CurrentVersion));
        OnPropertyChanged(nameof(Designation));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(TypeName));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(VersionNumber));
        OnPropertyChanged(nameof(StateName));
        OnPropertyChanged(nameof(Material));
        OnPropertyChanged(nameof(MassKg));
        OnPropertyChanged(nameof(MassDisplay));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanObsolete));
        OnPropertyChanged(nameof(CanCreateNewVersion));
    }

    private async Task ApproveAsync()
    {
        if (_item == null || _currentVersion == null) return;

        _currentVersion.Approve();
        await _itemRepository.SaveVersionAsync(_currentVersion);
        await _itemRepository.UpdateAsync(_item);

        SetItem(_item, _currentVersion);
        _onItemChanged();
    }

    private async Task ObsoleteAsync()
    {
        if (_item == null || _currentVersion == null) return;

        _currentVersion.Obsolete();
        _item.RecalculateCurrentVersion();

        await _itemRepository.SaveVersionAsync(_currentVersion);
        await _itemRepository.UpdateAsync(_item);

        var nextCurrent = _item.GetCurrentVersion();

        SetItem(_item, nextCurrent);
        _onItemChanged();
    }

    private async Task CreateNewVersionAsync()
    {
        if (_item == null || _currentVersion == null) return;

        var newVersion = _item.CreateNextVersion(_currentVersion.Material, _currentVersion.MassKg);
        await _itemRepository.SaveVersionAsync(newVersion);
        await _itemRepository.UpdateAsync(_item);

        // Копируем ссылки состава изделия от предыдущей версии
        var oldLinks = await _itemRepository.GetBomLinksByVersionIdAsync(_currentVersion.Id);
        var newLinks = oldLinks.Select(l => new BomLink(Guid.NewGuid(), newVersion.Id, l.ChildObjectId, l.Quantity));
        await _itemRepository.SaveBomLinksAsync(newVersion.Id, newLinks);

        SetItem(_item, newVersion);
        _onItemChanged();
    }
}


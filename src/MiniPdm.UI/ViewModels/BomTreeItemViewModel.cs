using System.Collections.ObjectModel;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class BomTreeItemViewModel : ViewModelBase
{
    private static readonly BomTreeItemViewModel DummyChild = new(Guid.Empty, ItemType.Part, null, "Загрузка...", null, 0);

    private readonly IBomQueryRepository? _queryRepository;
    private readonly bool _isRoot;
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isLoaded;

    public Guid ObjectId { get; }
    public ItemType Type { get; }
    public Designation? Designation { get; }
    public string Name { get; }
    public Guid? CurrentVersionId { get; }
    public int Quantity { get; }
    public bool IsRoot => _isRoot;

    public ObservableCollection<BomTreeItemViewModel> Children { get; } = new();

    public string DisplayText
    {
        get
        {
            var prefix = Designation.HasValue ? $"{Designation.Value} " : string.Empty;
            var qty = (!_isRoot && Quantity > 0) ? $" ×{Quantity}" : string.Empty;
            return $"{prefix}{Name}{qty}";
        }
    }

    public string TypeDisplayName => Type switch
    {
        ItemType.Assembly => "Сборка",
        ItemType.Part => "Деталь",
        ItemType.StandardPart => "Стандартное изделие",
        _ => Type.ToString()
    };

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value && !_isLoaded)
            {
                _ = LoadChildrenAsync();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public BomTreeItemViewModel(
        Guid objectId,
        ItemType type,
        Designation? designation,
        string name,
        Guid? currentVersionId,
        int quantity = 1,
        IBomQueryRepository? queryRepository = null,
        bool isRoot = false)
    {
        ObjectId = objectId;
        Type = type;
        Designation = designation;
        Name = name;
        CurrentVersionId = currentVersionId;
        Quantity = quantity;
        _queryRepository = queryRepository;
        _isRoot = isRoot;

        // Если это сборочная единица, добавляем dummy child для отображения стрелочки разворачивания
        if (type == ItemType.Assembly && currentVersionId.HasValue)
        {
            Children.Add(DummyChild);
        }
    }

    public async Task LoadChildrenAsync()
    {
        if (_isLoaded || _queryRepository == null || !CurrentVersionId.HasValue)
            return;

        try
        {
            var components = await _queryRepository.GetFirstLevelComponentsAsync(CurrentVersionId.Value);

            Children.Clear();
            foreach (var c in components)
            {
                var childVm = new BomTreeItemViewModel(
                    c.ObjectId,
                    c.Type,
                    c.Designation,
                    c.Name,
                    c.CurrentVersionId,
                    c.Quantity,
                    _queryRepository);

                Children.Add(childVm);
            }

            _isLoaded = true;
        }
        catch
        {
            // В случае ошибки убираем dummy child
            Children.Clear();
        }
    }
}


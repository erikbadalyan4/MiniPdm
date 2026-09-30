using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Exceptions;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Domain.Entities;

/// <summary>
/// Агрегат объекта PDM-системы (Сборочная единица, Деталь, Стандартное изделие).
/// </summary>
public class Item
{
    private readonly List<ItemVersion> _versions = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// Тип объекта.
    /// </summary>
    public ItemType Type { get; private set; }

    /// <summary>
    /// Обозначение по ЕСКД (уникально для деталей и сборок; NULL для стандартных изделий).
    /// </summary>
    public Designation? Designation { get; private set; }

    /// <summary>
    /// Наименование объекта (уникально для стандартных изделий).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Идентификатор текущей (последней неаннулированной) версии.
    /// </summary>
    public Guid? CurrentVersionId { get; private set; }

    /// <summary>
    /// Список всех версий объекта.
    /// </summary>
    public IReadOnlyCollection<ItemVersion> Versions => _versions.AsReadOnly();

    // Конструктор для Dapper / материализации
    private Item() { }

    private Item(Guid id, ItemType type, Designation? designation, string name)
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("Идентификатор объекта не может быть пустым.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Наименование объекта не может быть пустым.");

        ValidateTypeAndDesignation(type, designation);

        Id = id;
        Type = type;
        Designation = designation;
        Name = name.Trim();
    }

    /// <summary>
    /// Фабричный метод создания сборочной единицы.
    /// </summary>
    public static Item CreateAssembly(Guid id, Designation designation, string name) =>
        new(id, ItemType.Assembly, designation, name);

    /// <summary>
    /// Фабричный метод создания детали.
    /// </summary>
    public static Item CreatePart(Guid id, Designation designation, string name) =>
        new(id, ItemType.Part, designation, name);

    /// <summary>
    /// Фабричный метод создания стандартного изделия (без обозначения).
    /// </summary>
    public static Item CreateStandardPart(Guid id, string name) =>
        new(id, ItemType.StandardPart, null, name);

    /// <summary>
    /// Создание начальной версии при первой регистрации объекта.
    /// </summary>
    public ItemVersion CreateInitialVersion(string? material = null, decimal? massKg = null)
    {
        if (_versions.Count > 0)
            throw new DomainValidationException("Начальная версия уже существует.");

        ValidatePropertiesForType(material, massKg);

        var version = new ItemVersion(
            id: Guid.NewGuid(),
            objectId: Id,
            versionNumber: 1,
            state: VersionState.InWork,
            material: material,
            massKg: massKg);

        _versions.Add(version);
        CurrentVersionId = version.Id;
        return version;
    }

    /// <summary>
    /// Создание следующей версии (например, при повторном импорте утвержденного объекта).
    /// </summary>
    public ItemVersion CreateNextVersion(string? material = null, decimal? massKg = null)
    {
        ValidatePropertiesForType(material, massKg);

        var nextVersionNumber = _versions.Count == 0 ? 1 : _versions.Max(v => v.VersionNumber) + 1;

        var version = new ItemVersion(
            id: Guid.NewGuid(),
            objectId: Id,
            versionNumber: nextVersionNumber,
            state: VersionState.InWork,
            material: material,
            massKg: massKg);

        _versions.Add(version);
        RecalculateCurrentVersion();
        return version;
    }

    /// <summary>
    /// Получение текущей версии объекта.
    /// </summary>
    public ItemVersion? GetCurrentVersion()
    {
        if (!CurrentVersionId.HasValue)
            return null;

        return _versions.FirstOrDefault(v => v.Id == CurrentVersionId.Value);
    }

    /// <summary>
    /// Пересчет текущей версии (последняя неаннулированная версия по номеру).
    /// Вызывается при создании, аннулировании или изменении версий.
    /// </summary>
    public void RecalculateCurrentVersion()
    {
        var latestActive = _versions
            .Where(v => v.State != VersionState.Obsolete)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        CurrentVersionId = latestActive?.Id;
    }

    /// <summary>
    /// Гидратация версий при загрузке из базы данных.
    /// </summary>
    public void LoadVersions(IEnumerable<ItemVersion> versions)
    {
        _versions.Clear();
        _versions.AddRange(versions);
        RecalculateCurrentVersion();
    }

    private static void ValidateTypeAndDesignation(ItemType type, Designation? designation)
    {
        switch (type)
        {
            case ItemType.Assembly:
            case ItemType.Part:
                if (!designation.HasValue)
                    throw new DomainValidationException($"Для типа «{type}» обозначение по ЕСКД обязательно.");
                break;

            case ItemType.StandardPart:
                if (designation.HasValue)
                    throw new DomainValidationException("Для стандартного изделия обозначение должно отсутствовать (NULL).");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип объекта.");
        }
    }

    private void ValidatePropertiesForType(string? material, decimal? massKg)
    {
        if (Type == ItemType.Assembly)
        {
            if (!string.IsNullOrEmpty(material))
                throw new DomainValidationException("У сборочной единицы не может быть материала.");
            if (massKg.HasValue)
                throw new DomainValidationException("У сборочной единицы масса вычисляется по составу и не задается вручную.");
        }
        else if (Type == ItemType.StandardPart)
        {
            if (!string.IsNullOrEmpty(material))
                throw new DomainValidationException("У стандартного изделия не указывается материал (он заложен в наименовании по ГОСТ).");
        }
    }
}


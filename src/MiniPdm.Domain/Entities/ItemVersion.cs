using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Exceptions;

namespace MiniPdm.Domain.Entities;

/// <summary>
/// Версия объекта PDM и её атрибуты.
/// </summary>
public class ItemVersion
{
    private readonly List<BomLink> _bomLinks = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// Идентификатор объекта, которому принадлежит данная версия.
    /// </summary>
    public Guid ObjectId { get; private set; }

    /// <summary>
    /// Номер версии (1, 2, 3...). Уникален в пределах объекта.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Состояние жизненного цикла версии (В работе, Утверждено, Аннулировано).
    /// </summary>
    public VersionState State { get; private set; }

    /// <summary>
    /// Материал (только у деталей).
    /// </summary>
    public string? Material { get; private set; }

    /// <summary>
    /// Масса в кг за 1 шт.
    /// </summary>
    public decimal? MassKg { get; private set; }

    /// <summary>
    /// Дата и время создания версии.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Дочерние связи состава изделия («Состоит из ...»).
    /// </summary>
    public IReadOnlyCollection<BomLink> BomLinks => _bomLinks.AsReadOnly();

    // Конструктор для Dapper / материализации
    private ItemVersion() { }

    public ItemVersion(
        Guid id,
        Guid objectId,
        int versionNumber,
        VersionState state = VersionState.InWork,
        string? material = null,
        decimal? massKg = null,
        DateTime? createdAt = null)
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("Идентификатор версии не может быть пустым.");

        if (objectId == Guid.Empty)
            throw new DomainValidationException("Идентификатор объекта не может быть пустым.");

        if (versionNumber <= 0)
            throw new DomainValidationException($"Номер версии должен быть положительным числом. Получено: {versionNumber}.");

        if (massKg < 0)
            throw new DomainValidationException($"Масса не может быть отрицательной. Получено: {massKg}.");

        Id = id;
        ObjectId = objectId;
        VersionNumber = versionNumber;
        State = state;
        Material = material?.Trim();
        MassKg = massKg;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Перевод версии в состояние «Утверждено».
    /// </summary>
    public void Approve()
    {
        if (State != VersionState.InWork)
            throw new InvalidStateTransitionException(State, VersionState.Approved);

        State = VersionState.Approved;
    }

    /// <summary>
    /// Перевод версии в состояние «Аннулировано».
    /// </summary>
    public void Obsolete()
    {
        if (State == VersionState.Obsolete)
            return; // Уже аннулировано

        // Разрешено только из InWork и Approved
        if (State != VersionState.InWork && State != VersionState.Approved)
            throw new InvalidStateTransitionException(State, VersionState.Obsolete);

        State = VersionState.Obsolete;
    }

    /// <summary>
    /// Обновление атрибутов версии (разрешено только в состоянии «В работе»).
    /// </summary>
    public void UpdateProperties(string? material, decimal? massKg)
    {
        EnsureInWork();

        if (massKg < 0)
            throw new DomainValidationException($"Масса не может быть отрицательной. Получено: {massKg}.");

        Material = material?.Trim();
        MassKg = massKg;
    }

    /// <summary>
    /// Добавление или обновление количества дочернего компонента в составе.
    /// </summary>
    public void AddOrUpdateComponent(Guid childObjectId, int quantity)
    {
        EnsureInWork();

        var existing = _bomLinks.FirstOrDefault(l => l.ChildObjectId == childObjectId);
        if (existing != null)
        {
            existing.UpdateQuantity(quantity);
        }
        else
        {
            _bomLinks.Add(new BomLink(Guid.NewGuid(), Id, childObjectId, quantity));
        }
    }

    /// <summary>
    /// Удаление компонента из состава.
    /// </summary>
    public void RemoveComponent(Guid childObjectId)
    {
        EnsureInWork();
        _bomLinks.RemoveAll(l => l.ChildObjectId == childObjectId);
    }

    /// <summary>
    /// Очистка всех связей состава.
    /// </summary>
    public void ClearComponents()
    {
        EnsureInWork();
        _bomLinks.Clear();
    }

    /// <summary>
    /// Внутреннее заполнение связей при гидратации из репозитория.
    /// </summary>
    public void LoadBomLinks(IEnumerable<BomLink> links)
    {
        _bomLinks.Clear();
        _bomLinks.AddRange(links);
    }

    private void EnsureInWork()
    {
        if (State != VersionState.InWork)
            throw new DomainValidationException($"Изменение версии невозможно: версия находится в состоянии «{State}». Правки допускаются только в состоянии «В работе».");
    }
}


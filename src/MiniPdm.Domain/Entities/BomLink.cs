using MiniPdm.Domain.Exceptions;

namespace MiniPdm.Domain.Entities;

/// <summary>
/// Связь вхождения в состав изделия («Состоит из ...»).
/// Связывает конкретную версию сборки с дочерним объектом (не конкретной версией).
/// </summary>
public class BomLink
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Идентификатор версии сборки-родителя.
    /// </summary>
    public Guid ParentVersionId { get; private set; }

    /// <summary>
    /// Идентификатор дочернего объекта (деталь, стандартное изделие или подсборка).
    /// </summary>
    public Guid ChildObjectId { get; private set; }

    /// <summary>
    /// Количество (целое положительное число > 0).
    /// </summary>
    public int Quantity { get; private set; }

    // Конструктор для Dapper / ORM
    private BomLink() { }

    public BomLink(Guid id, Guid parentVersionId, Guid childObjectId, int quantity)
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("Идентификатор связи состава не может быть пустым.");

        if (parentVersionId == Guid.Empty)
            throw new DomainValidationException("Идентификатор родительской версии не может быть пустым.");

        if (childObjectId == Guid.Empty)
            throw new DomainValidationException("Идентификатор дочернего объекта не может быть пустым.");

        if (quantity <= 0)
            throw new DomainValidationException($"Количество компонента должно быть строго больше 0. Получено: {quantity}.");

        Id = id;
        ParentVersionId = parentVersionId;
        ChildObjectId = childObjectId;
        Quantity = quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new DomainValidationException($"Количество компонента должно быть строго больше 0. Получено: {newQuantity}.");

        Quantity = newQuantity;
    }
}


namespace MiniPdm.Application.Services;

public interface ICycleDetector
{
    /// <summary>
    /// Проверяет наличие циклических ссылок в графе состава изделия.
    /// </summary>
    /// <param name="rootId">Идентификатор корневого объекта</param>
    /// <param name="childrenProvider">Функция получения дочерних идентификаторов для заданного объекта</param>
    /// <param name="cyclePath">Обнаруженный циклический путь</param>
    /// <returns>True, если обнаружен цикл</returns>
    bool DetectCycle(
        Guid rootId,
        Func<Guid, IEnumerable<Guid>> childrenProvider,
        out IReadOnlyList<Guid> cyclePath);

    /// <summary>
    /// Проверяет, приведет ли добавление связи между parentId и childId к циклу.
    /// </summary>
    bool WouldCreateCycle(
        Guid parentId,
        Guid childId,
        Func<Guid, IEnumerable<Guid>> childrenProvider);
}

namespace MiniPdm.Domain.Enums;

/// <summary>
/// Состояние (жизненный цикл) версии объекта.
/// </summary>
public enum VersionState
{
    /// <summary>
    /// «В работе» — версию можно изменять.
    /// </summary>
    InWork = 1,

    /// <summary>
    /// «Утверждено» — версию изменять нельзя, правки только через новую версию.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// «Аннулировано» — версия не участвует в расчетах.
    /// </summary>
    Obsolete = 3
}


using MiniPdm.Domain.Enums;

namespace MiniPdm.Domain.Exceptions;

/// <summary>
/// Исключение при попытке недопустимого перехода жизненного цикла версии.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public VersionState FromState { get; }
    public VersionState ToState { get; }

    public InvalidStateTransitionException(VersionState fromState, VersionState toState)
        : base($"Недопустимый переход состояния версии: с «{fromState}» на «{toState}». Обратные переходы запрещены.")
    {
        FromState = fromState;
        ToState = toState;
    }
}


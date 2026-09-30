namespace MiniPdm.Domain.Exceptions;

/// <summary>
/// Базовое исключение доменной модели.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}


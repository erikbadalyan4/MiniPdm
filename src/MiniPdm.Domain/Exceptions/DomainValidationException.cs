namespace MiniPdm.Domain.Exceptions;

/// <summary>
/// Исключение нарушения инвариантов бизнес-правил домена.
/// </summary>
public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}


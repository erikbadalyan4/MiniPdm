using System.Text.RegularExpressions;
using MiniPdm.Domain.Exceptions;

namespace MiniPdm.Domain.ValueObjects;

/// <summary>
/// Обозначение по ЕСКД (формат: 4 кириллические заглавные буквы, точка, 6 цифр, точка, 3 цифры).
/// Пример: РДЦЛ.304112.300
/// </summary>
public readonly partial record struct Designation
{
    // Скомпилированное регулярное выражение .NET 8 (Source Generator) для максимальной производительности
    [GeneratedRegex(@"^[А-ЯЁ]{4}\.\d{6}\.\d{3}$")]
    private static partial Regex EskdRegex();

    public string Value { get; }

    private Designation(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Создает объект обозначения или выбрасывает исключение валидации.
    /// </summary>
    public static Designation Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Обозначение не может быть пустым.");
        }

        var trimmed = value.Trim();
        if (!EskdRegex().IsMatch(trimmed))
        {
            throw new DomainValidationException(
                $"Обозначение «{value}» не соответствует формату ЕСКД (ожидается: АБВГ.123456.789).");
        }

        return new Designation(trimmed);
    }

    /// <summary>
    /// Безопасная попытка создания обозначения без исключения.
    /// </summary>
    public static bool TryCreate(string? value, out Designation designation, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            designation = default;
            error = "Обозначение не задано.";
            return false;
        }

        var trimmed = value.Trim();
        if (!EskdRegex().IsMatch(trimmed))
        {
            designation = default;
            error = $"Обозначение «{value}» не соответствует формату ЕСКД (формат: АБВГ.123456.789).";
            return false;
        }

        designation = new Designation(trimmed);
        error = null;
        return true;
    }

    /// <summary>
    /// Проверка соответствия формату ЕСКД.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && EskdRegex().IsMatch(value.Trim());

    public override string ToString() => Value;

    public static implicit operator string(Designation designation) => designation.Value;
}


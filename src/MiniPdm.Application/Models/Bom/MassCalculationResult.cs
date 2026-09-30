namespace MiniPdm.Application.Models.Bom;

/// <summary>
/// Результат расчета массы сборки.
/// Если хотя бы у одной детали или стандартного изделия в составе отсутствует масса,
/// расчет возвращает список проблемных компонентов, а не неполную сумму.
/// </summary>
public sealed class MassCalculationResult
{
    public bool IsSuccess { get; }
    public decimal? TotalMassKg { get; }
    public IReadOnlyList<string> MissingMassComponents { get; }
    public string? ErrorMessage { get; }

    private MassCalculationResult(bool isSuccess, decimal? totalMassKg, IReadOnlyList<string> missingMassComponents, string? errorMessage)
    {
        IsSuccess = isSuccess;
        TotalMassKg = totalMassKg;
        MissingMassComponents = missingMassComponents;
        ErrorMessage = errorMessage;
    }

    public static MassCalculationResult Success(decimal totalMassKg) =>
        new(true, totalMassKg, Array.Empty<string>(), null);

    public static MassCalculationResult MissingMass(IEnumerable<string> missingComponents)
    {
        var list = missingComponents.Distinct().ToList();
        var message = $"Невозможно рассчитать массу сборки: отсутствует масса у компонентов: {string.Join(", ", list)}";
        return new(false, null, list, message);
    }

    public static MassCalculationResult Failure(string error) =>
        new(false, null, Array.Empty<string>(), error);
}


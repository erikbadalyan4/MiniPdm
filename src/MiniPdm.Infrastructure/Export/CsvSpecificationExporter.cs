using System.Text;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Infrastructure.Export;

public interface ISpecificationExporter
{
    Task ExportToCsvAsync(IEnumerable<ConsolidatedBomItem> items, string filePath, CancellationToken ct = default);
    string GenerateCsvString(IEnumerable<ConsolidatedBomItem> items);
}

public sealed class CsvSpecificationExporter : ISpecificationExporter
{
    public async Task ExportToCsvAsync(IEnumerable<ConsolidatedBomItem> items, string filePath, CancellationToken ct = default)
    {
        var csv = GenerateCsvString(items);
        await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8, ct);
    }

    public string GenerateCsvString(IEnumerable<ConsolidatedBomItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Тип;Обозначение;Наименование;Материал;Количество;Масса за 1 шт (кг);Общая масса (кг)");

        foreach (var item in items)
        {
            var typeName = item.Type switch
            {
                ItemType.Assembly => "Сборочная единица",
                ItemType.Part => "Деталь",
                ItemType.StandardPart => "Стандартное изделие",
                _ => item.Type.ToString()
            };

            var designation = EscapeCsv(item.Designation?.Value ?? string.Empty);
            var name = EscapeCsv(item.Name);
            var material = EscapeCsv(item.Material ?? string.Empty);
            var qty = item.TotalQuantity.ToString();
            var unitMass = item.UnitMassKg.HasValue ? item.UnitMassKg.Value.ToString("F3") : string.Empty;
            var totalMass = item.TotalMassKg.HasValue ? item.TotalMassKg.Value.ToString("F3") : string.Empty;

            sb.AppendLine($"{typeName};{designation};{name};{material};{qty};{unitMass};{totalMass}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}


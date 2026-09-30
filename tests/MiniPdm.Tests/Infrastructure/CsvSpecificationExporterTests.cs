using FluentAssertions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;
using MiniPdm.Infrastructure.Export;

namespace MiniPdm.Tests.Infrastructure;

public class CsvSpecificationExporterTests
{
    private readonly CsvSpecificationExporter _exporter = new();

    [Fact]
    public void GenerateCsvString_ShouldFormatColumnsAndRowsCorrectly()
    {
        var items = new List<ConsolidatedBomItem>
        {
            new(Guid.NewGuid(), ItemType.Part, Designation.Create("АБВГ.111111.001"), "Вал", 2, 5.5m, "Сталь 45"),
            new(Guid.NewGuid(), ItemType.StandardPart, null, "Болт М8", 10, 0.05m, null)
        };

        var csv = _exporter.GenerateCsvString(items);

        csv.Should().Contain("Тип;Обозначение;Наименование;Материал;Количество;Масса за 1 шт (кг);Общая масса (кг)");
        csv.Should().Contain("Деталь;АБВГ.111111.001;Вал;Сталь 45;2;5,500;11,000");
        csv.Should().Contain("Стандартное изделие;;Болт М8;;10;0,050;0,500");
    }
}


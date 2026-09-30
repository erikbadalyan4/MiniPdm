using FluentAssertions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Tests.Application;

public class BomCalculationServiceTests
{
    private readonly BomCalculationService _service = new();

    [Fact]
    public void CalculateMass_WhenAllPartsHaveMass_ShouldCalculateTotalCorrectly()
    {
        var rootId = Guid.NewGuid();
        var part1Id = Guid.NewGuid();
        var subAssemblyId = Guid.NewGuid();
        var part2Id = Guid.NewGuid();

        // Дерево:
        // Root
        // ├── Part1 (qty 2, mass 5.0 kg)
        // └── SubAssembly (qty 3)
        //     └── Part2 (qty 4, mass 1.5 kg)
        // Итого Part1: 2 * 5.0 = 10.0
        // Итого Part2: 3 * 4 * 1.5 = 18.0
        // Всего: 28.0 kg
        var nodes = new List<BomHierarchyNode>
        {
            new(rootId, ItemType.Assembly, Designation.Create("АБВГ.100000.001"), "Главная сборка", Guid.NewGuid(), 1, VersionState.Approved, null, null, 1, 0, null, null, "/1/"),
            new(part1Id, ItemType.Part, Designation.Create("АБВГ.200000.001"), "Деталь 1", Guid.NewGuid(), 1, VersionState.Approved, "Сталь", 5.0m, 2, 1, rootId, null, "/1/2/"),
            new(subAssemblyId, ItemType.Assembly, Designation.Create("АБВГ.300000.001"), "Подсборка", Guid.NewGuid(), 1, VersionState.Approved, null, null, 3, 1, rootId, null, "/1/3/"),
            new(part2Id, ItemType.Part, Designation.Create("АБВГ.200000.002"), "Деталь 2", Guid.NewGuid(), 1, VersionState.Approved, "Медь", 1.5m, 4, 2, subAssemblyId, null, "/1/3/4/")
        };

        var result = _service.CalculateMass(nodes);

        result.IsSuccess.Should().BeTrue();
        result.TotalMassKg.Should().Be(28.0m);
        result.MissingMassComponents.Should().BeEmpty();
    }

    [Fact]
    public void CalculateMass_WhenPartHasMissingMass_ShouldReturnMissingComponentsList()
    {
        var rootId = Guid.NewGuid();
        var partWithoutMassId = Guid.NewGuid();

        var nodes = new List<BomHierarchyNode>
        {
            new(rootId, ItemType.Assembly, Designation.Create("АБВГ.100000.001"), "Главная сборка", Guid.NewGuid(), 1, VersionState.Approved, null, null, 1, 0, null, null, "/1/"),
            new(partWithoutMassId, ItemType.Part, Designation.Create("АБВГ.200000.001"), "Крышка смотровая", Guid.NewGuid(), 1, VersionState.Approved, "Сталь", null, 2, 1, rootId, null, "/1/2/")
        };

        var result = _service.CalculateMass(nodes);

        result.IsSuccess.Should().BeFalse();
        result.TotalMassKg.Should().BeNull();
        result.MissingMassComponents.Should().ContainSingle()
            .Which.Should().Contain("Крышка смотровая");
    }

    [Fact]
    public void BuildConsolidatedSpecification_WhenSameItemOnDifferentLevels_ShouldMultiplyAndSumQuantities()
    {
        var rootId = Guid.NewGuid();
        var boltId = Guid.NewGuid();
        var subAssemblyId = Guid.NewGuid();

        // Болт М8 входит в корень (qty 4) и в подсборку (qty 2). Сама подсборка входит в корень 3 раза.
        // Итого болтов: 4 + (3 * 2) = 10 шт.
        var nodes = new List<BomHierarchyNode>
        {
            new(rootId, ItemType.Assembly, Designation.Create("АБВГ.100000.001"), "Главная сборка", Guid.NewGuid(), 1, VersionState.Approved, null, null, 1, 0, null, null, "/1/"),
            new(boltId, ItemType.StandardPart, null, "Болт М8 ГОСТ 7798-70", Guid.NewGuid(), 1, VersionState.Approved, null, 0.05m, 4, 1, rootId, null, "/1/2/"),
            new(subAssemblyId, ItemType.Assembly, Designation.Create("АБВГ.300000.001"), "Подсборка", Guid.NewGuid(), 1, VersionState.Approved, null, null, 3, 1, rootId, null, "/1/3/"),
            new(boltId, ItemType.StandardPart, null, "Болт М8 ГОСТ 7798-70", Guid.NewGuid(), 1, VersionState.Approved, null, 0.05m, 2, 2, subAssemblyId, null, "/1/3/4/")
        };

        var spec = _service.BuildConsolidatedSpecification(nodes);

        spec.Should().ContainSingle();
        var bolt = spec.First();
        bolt.Name.Should().Be("Болт М8 ГОСТ 7798-70");
        bolt.TotalQuantity.Should().Be(10);
        bolt.TotalMassKg.Should().Be(0.50m);
    }
}


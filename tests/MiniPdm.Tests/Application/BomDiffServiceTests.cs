using FluentAssertions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Application.Services;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Tests.Application;

public class BomDiffServiceTests
{
    private readonly BomDiffService _service = new();

    [Fact]
    public void CompareVersions_ShouldDetectAddedRemovedModifiedAndUnchanged()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        var partRemoved = Item.CreatePart(Guid.NewGuid(), Designation.Create("АБВГ.111111.001"), "Удаленная деталь");
        var partModified = Item.CreatePart(Guid.NewGuid(), Designation.Create("АБВГ.222222.001"), "Измененная деталь");
        var partUnchanged = Item.CreatePart(Guid.NewGuid(), Designation.Create("АБВГ.333333.001"), "Без изменений");
        var partAdded = Item.CreatePart(Guid.NewGuid(), Designation.Create("АБВГ.444444.001"), "Новая деталь");

        var items = new Dictionary<Guid, Item>
        {
            [partRemoved.Id] = partRemoved,
            [partModified.Id] = partModified,
            [partUnchanged.Id] = partUnchanged,
            [partAdded.Id] = partAdded
        };

        var oldLinks = new List<BomLink>
        {
            new(Guid.NewGuid(), v1, partRemoved.Id, 1),
            new(Guid.NewGuid(), v1, partModified.Id, 2),
            new(Guid.NewGuid(), v1, partUnchanged.Id, 5)
        };

        var newLinks = new List<BomLink>
        {
            new(Guid.NewGuid(), v2, partModified.Id, 4), // количество изменилось 2 -> 4
            new(Guid.NewGuid(), v2, partUnchanged.Id, 5), // без изменений
            new(Guid.NewGuid(), v2, partAdded.Id, 3)     // добавлена
        };

        var diff = _service.CompareVersions(oldLinks, newLinks, id => items.GetValueOrDefault(id));

        diff.AddedCount.Should().Be(1);
        diff.RemovedCount.Should().Be(1);
        diff.ModifiedCount.Should().Be(1);

        diff.Changes.Should().Contain(c => c.Kind == BomDiffKind.Added && c.ObjectId == partAdded.Id && c.NewQuantity == 3);
        diff.Changes.Should().Contain(c => c.Kind == BomDiffKind.Removed && c.ObjectId == partRemoved.Id && c.OldQuantity == 1);
        diff.Changes.Should().Contain(c => c.Kind == BomDiffKind.QuantityChanged && c.ObjectId == partModified.Id && c.OldQuantity == 2 && c.NewQuantity == 4);
        diff.Changes.Should().Contain(c => c.Kind == BomDiffKind.Unchanged && c.ObjectId == partUnchanged.Id && c.NewQuantity == 5);
    }
}


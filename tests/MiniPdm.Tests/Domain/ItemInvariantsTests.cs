using FluentAssertions;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Exceptions;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Tests.Domain;

public class ItemInvariantsTests
{
    private readonly Designation _validDesignation = Designation.Create("РДЦЛ.304112.300");

    [Fact]
    public void CreateAssembly_WithDesignationAndName_ShouldSucceed()
    {
        var item = Item.CreateAssembly(Guid.NewGuid(), _validDesignation, "Вал в сборе");

        item.Type.Should().Be(ItemType.Assembly);
        item.Designation.Should().Be(_validDesignation);
        item.Name.Should().Be("Вал в сборе");
        item.CurrentVersionId.Should().BeNull();
    }

    [Fact]
    public void CreateStandardPart_ShouldNotHaveDesignation()
    {
        var item = Item.CreateStandardPart(Guid.NewGuid(), "Болт М12х40 ГОСТ 7798-70");

        item.Type.Should().Be(ItemType.StandardPart);
        item.Designation.Should().BeNull();
        item.Name.Should().Be("Болт М12х40 ГОСТ 7798-70");
    }

    [Fact]
    public void CreateInitialVersion_ShouldSetCurrentVersionId()
    {
        var item = Item.CreatePart(Guid.NewGuid(), _validDesignation, "Вал");
        var version = item.CreateInitialVersion("Сталь 45", 12.5m);

        version.VersionNumber.Should().Be(1);
        item.CurrentVersionId.Should().Be(version.Id);
        item.GetCurrentVersion().Should().Be(version);
    }

    [Fact]
    public void CreateNextVersion_ShouldIncrementVersionNumberAndSetCurrent()
    {
        var item = Item.CreatePart(Guid.NewGuid(), _validDesignation, "Вал");
        var v1 = item.CreateInitialVersion("Сталь 45", 12.5m);
        v1.Approve();

        var v2 = item.CreateNextVersion("Сталь 40Х", 12.0m);

        v2.VersionNumber.Should().Be(2);
        item.CurrentVersionId.Should().Be(v2.Id);
        item.Versions.Should().HaveCount(2);
    }

    [Fact]
    public void ObsoleteCurrentVersion_ShouldUpdateCurrentVersionToLatestNonObsolete()
    {
        var item = Item.CreatePart(Guid.NewGuid(), _validDesignation, "Вал");
        var v1 = item.CreateInitialVersion("Сталь 45", 12.5m);
        v1.Approve();
        var v2 = item.CreateNextVersion("Сталь 40Х", 12.0m);

        // Аннулируем вторую версию
        v2.Obsolete();
        item.RecalculateCurrentVersion();

        item.CurrentVersionId.Should().Be(v1.Id);
        item.GetCurrentVersion().Should().Be(v1);
    }

    [Fact]
    public void Obsolete_WhenAllVersionsObsolete_GetCurrentVersionShouldReturnLatestArchivedVersion()
    {
        var item = Item.CreatePart(Guid.NewGuid(), _validDesignation, "Вал");
        var v1 = item.CreateInitialVersion("Сталь 45", 12.5m);

        v1.Obsolete();
        item.RecalculateCurrentVersion();

        item.CurrentVersionId.Should().BeNull();
        item.GetCurrentVersion().Should().Be(v1);
        item.GetCurrentVersion()!.State.Should().Be(VersionState.Obsolete);
    }

    [Fact]
    public void Assembly_ShouldNotAllowMaterialOrManualMass()
    {
        var item = Item.CreateAssembly(Guid.NewGuid(), _validDesignation, "Редуктор");

        var act1 = () => item.CreateInitialVersion(material: "Сталь", massKg: null);
        var act2 = () => item.CreateInitialVersion(material: null, massKg: 50.0m);

        act1.Should().Throw<DomainValidationException>();
        act2.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void BomLink_WithQuantityNotGreaterThanZero_ShouldThrowDomainValidationException(int invalidQty)
    {
        var act = () => new BomLink(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), invalidQty);

        act.Should().Throw<DomainValidationException>()
            .WithMessage("*строго больше 0*");
    }
}

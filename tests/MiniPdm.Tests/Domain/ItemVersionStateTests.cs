using FluentAssertions;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Exceptions;

namespace MiniPdm.Tests.Domain;

public class ItemVersionStateTests
{
    [Fact]
    public void NewVersion_ShouldHaveInWorkState()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);

        version.State.Should().Be(VersionState.InWork);
    }

    [Fact]
    public void Approve_FromInWork_ShouldTransitionToApproved()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);

        version.Approve();

        version.State.Should().Be(VersionState.Approved);
    }

    [Fact]
    public void Obsolete_FromInWork_ShouldTransitionToObsolete()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);

        version.Obsolete();

        version.State.Should().Be(VersionState.Obsolete);
    }

    [Fact]
    public void Obsolete_FromApproved_ShouldTransitionToObsolete()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);
        version.Approve();

        version.Obsolete();

        version.State.Should().Be(VersionState.Obsolete);
    }

    [Fact]
    public void Approve_FromApproved_ShouldThrowInvalidStateTransitionException()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);
        version.Approve();

        var act = () => version.Approve();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Approve_FromObsolete_ShouldThrowInvalidStateTransitionException()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);
        version.Obsolete();

        var act = () => version.Approve();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void UpdateProperties_WhenApproved_ShouldThrowDomainValidationException()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);
        version.Approve();

        var act = () => version.UpdateProperties("Сталь 45", 10.5m);

        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Правки допускаются только в состоянии «В работе»*");
    }

    [Fact]
    public void AddComponent_WhenApproved_ShouldThrowDomainValidationException()
    {
        var version = new ItemVersion(Guid.NewGuid(), Guid.NewGuid(), 1);
        version.Approve();

        var act = () => version.AddOrUpdateComponent(Guid.NewGuid(), 2);

        act.Should().Throw<DomainValidationException>();
    }
}

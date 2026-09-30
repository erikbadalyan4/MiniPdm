using FluentAssertions;
using MiniPdm.Application.Services;

namespace MiniPdm.Tests.Application;

public class CycleDetectorTests
{
    private readonly CycleDetector _detector = new();

    [Fact]
    public void DetectCycle_WhenNoCycles_ShouldReturnFalse()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var graph = new Dictionary<Guid, List<Guid>>
        {
            [a] = new() { b, c },
            [b] = new(),
            [c] = new()
        };

        var hasCycle = _detector.DetectCycle(a, id => graph.GetValueOrDefault(id) ?? new(), out var cyclePath);

        hasCycle.Should().BeFalse();
        cyclePath.Should().BeEmpty();
    }

    [Fact]
    public void DetectCycle_WhenDirectSelfReference_ShouldDetectCycle()
    {
        var a = Guid.NewGuid();
        var graph = new Dictionary<Guid, List<Guid>>
        {
            [a] = new() { a }
        };

        var hasCycle = _detector.DetectCycle(a, id => graph.GetValueOrDefault(id) ?? new(), out var cyclePath);

        hasCycle.Should().BeTrue();
        cyclePath.Should().Contain(a);
    }

    [Fact]
    public void DetectCycle_WhenIndirectCycle_ShouldDetectCycleAndReturnPath()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var graph = new Dictionary<Guid, List<Guid>>
        {
            [a] = new() { b },
            [b] = new() { c },
            [c] = new() { a }
        };

        var hasCycle = _detector.DetectCycle(a, id => graph.GetValueOrDefault(id) ?? new(), out var cyclePath);

        hasCycle.Should().BeTrue();
        cyclePath.Should().HaveCount(4); // a -> b -> c -> a
        cyclePath.First().Should().Be(cyclePath.Last());
    }

    [Fact]
    public void WouldCreateCycle_WhenAddingBackLink_ShouldReturnTrue()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var graph = new Dictionary<Guid, List<Guid>>
        {
            [a] = new() { b },
            [b] = new() { c },
            [c] = new()
        };

        var createsCycle = _detector.WouldCreateCycle(c, a, id => graph.GetValueOrDefault(id) ?? new());

        createsCycle.Should().BeTrue();
    }
}

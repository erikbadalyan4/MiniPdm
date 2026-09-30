using FluentAssertions;
using MiniPdm.Infrastructure.Cad;

namespace MiniPdm.Tests.Infrastructure;

public class JsonCadDocumentReaderTests
{
    private readonly JsonCadDocumentReader _reader = new();

    [Fact]
    public async Task ReadAsync_WhenValidAssemblyFile_ShouldParseAllFields()
    {
        var basePath = AppContext.BaseDirectory;
        // Находим папку cad-export относительно бинарников тестов
        var cadPath = Path.GetFullPath(Path.Combine(basePath, "../../../../cad-export/Вал промежуточный в сборе.a3d"));

        if (!File.Exists(cadPath))
            return; // Пропуск если запуск вне корня решения

        var doc = await _reader.ReadAsync(cadPath);

        doc.FileName.Should().Be("Вал промежуточный в сборе.a3d");
        doc.Type.Should().Be("Assembly");
        doc.Designation.Should().Be("РДЦЛ.304112.300");
        doc.Name.Should().Be("Вал промежуточный в сборе");
        doc.Components.Should().HaveCount(5);
        doc.Components.Should().Contain(c => c.File == "Колесо зубчатое.m3d" && c.Count == 1);
        doc.Components.Should().Contain(c => c.File == "Подшипник 208 ГОСТ 8338-75.m3d" && c.Count == 2);
    }

    [Fact]
    public async Task ReadAsync_WhenValidPartFile_ShouldParseProperties()
    {
        var basePath = AppContext.BaseDirectory;
        var cadPath = Path.GetFullPath(Path.Combine(basePath, "../../../../cad-export/Колесо зубчатое.m3d"));

        if (!File.Exists(cadPath))
            return;

        var doc = await _reader.ReadAsync(cadPath);

        doc.Type.Should().Be("Part");
        doc.Designation.Should().Be("РДЦЛ.304112.302");
        doc.Name.Should().Be("Колесо зубчатое");
        doc.Properties.Material.Should().Be("Сталь 40Х");
        doc.Properties.Mass.Should().Be(5.86m);
        doc.Components.Should().BeEmpty();
    }
}


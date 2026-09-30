using System.Text.Json;
using System.Text.Json.Serialization;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Cad;

namespace MiniPdm.Infrastructure.Cad;

public sealed class JsonCadDocumentReader : ICadDocumentReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<CadDocument> ReadAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл документа не найден: {path}");

        await using var stream = File.OpenRead(path);
        var dto = await JsonSerializer.DeserializeAsync<CadFileDto>(stream, SerializerOptions, ct);

        if (dto == null)
            throw new InvalidOperationException($"Не удалось десериализовать документ: {path}");

        var components = (dto.Components ?? Enumerable.Empty<CadComponentDto>())
            .Select(c => new CadComponent(c.File ?? string.Empty, c.Count))
            .ToList();

        var props = new CadProperties(dto.Properties?.Material, dto.Properties?.Mass);

        var fileName = !string.IsNullOrWhiteSpace(dto.FileName)
            ? dto.FileName
            : Path.GetFileName(path);

        return new CadDocument(
            dto.FormatVersion,
            fileName,
            dto.Type ?? string.Empty,
            dto.Designation,
            dto.Name ?? string.Empty,
            props,
            components);
    }

    private sealed class CadFileDto
    {
        public int FormatVersion { get; set; } = 1;
        public string? FileName { get; set; }
        public string? Type { get; set; }
        public string? Designation { get; set; }
        public string? Name { get; set; }
        public CadPropertiesDto? Properties { get; set; }
        public List<CadComponentDto>? Components { get; set; }
    }

    private sealed class CadPropertiesDto
    {
        public string? Material { get; set; }
        public decimal? Mass { get; set; }
    }

    private sealed class CadComponentDto
    {
        public string? File { get; set; }
        public int Count { get; set; }
    }
}


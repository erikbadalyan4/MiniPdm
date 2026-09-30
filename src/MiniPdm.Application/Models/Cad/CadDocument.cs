namespace MiniPdm.Application.Models.Cad;

public sealed record CadDocument(
    int FormatVersion,
    string FileName,
    string Type,
    string? Designation,
    string Name,
    CadProperties Properties,
    IReadOnlyList<CadComponent> Components);

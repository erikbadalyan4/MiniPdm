using MiniPdm.Application.Models.Cad;

namespace MiniPdm.Application.Abstractions;

/// <summary>
/// Абстракция чтения документов САПР.
/// Позволяет изолировать домен и бизнес-логику от конкретного формата хранения (JSON/API САПР).
/// </summary>
public interface ICadDocumentReader
{
    Task<CadDocument> ReadAsync(string path, CancellationToken ct = default);
}


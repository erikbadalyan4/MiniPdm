namespace MiniPdm.Application.Models.Import;

public sealed record ImportItemResult(
    string FileName,
    ImportSeverity Severity,
    string Reason);

namespace MiniPdm.Application.Models.Import;

public sealed class ImportReport
{
    public IReadOnlyList<ImportItemResult> Items { get; }
    public int AcceptedCount { get; }
    public int RejectedCount { get; }
    public int WarningCount { get; }

    public ImportReport(IReadOnlyList<ImportItemResult> items)
    {
        Items = items;
        AcceptedCount = items.Count(x => x.Severity != ImportSeverity.Error);
        RejectedCount = items.Count(x => x.Severity == ImportSeverity.Error);
        WarningCount = items.Count(x => x.Severity == ImportSeverity.Warning);
    }
}


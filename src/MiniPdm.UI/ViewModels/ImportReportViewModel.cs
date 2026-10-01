using System.Collections.ObjectModel;
using MiniPdm.Application.Models.Import;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class ImportReportViewModel : ViewModelBase
{
    public ImportReport Report { get; }

    public ObservableCollection<ImportItemResult> Items { get; } = new();

    public string SummaryText =>
        $"Итог импорта: принято {Report.AcceptedCount}, отклонено {Report.RejectedCount}, с предупреждением {Report.WarningCount}";

    public ImportReportViewModel(ImportReport report)
    {
        Report = report;
        foreach (var item in report.Items)
        {
            Items.Add(item);
        }
    }
}


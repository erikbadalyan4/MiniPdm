using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using MiniPdm.Application.Models.Import;
using MiniPdm.UI.Common;

namespace MiniPdm.UI.ViewModels;

public sealed class ImportReportItemViewModel
{
    public string FileName { get; }
    public ImportSeverity Severity { get; }

    public string SeverityText => Severity switch
    {
        ImportSeverity.Error => "Ошибка",
        ImportSeverity.Warning => "Предупреждение",
        ImportSeverity.Success => "Принято",
        _ => Severity.ToString()
    };

    public string SeverityColor => Severity switch
    {
        ImportSeverity.Error => "#DC2626", // Red-600
        ImportSeverity.Warning => "#D97706", // Amber-600
        ImportSeverity.Success => "#16A34A", // Green-600
        _ => "#0F172A"
    };

    public string StatusIcon => Severity switch
    {
        ImportSeverity.Error => "✖",
        ImportSeverity.Warning => "⚠",
        ImportSeverity.Success => "✔",
        _ => string.Empty
    };

    public string StatusIconColor => SeverityColor;

    public string Reason { get; }

    public ImportReportItemViewModel(string fileName, ImportSeverity severity, string reason)
    {
        FileName = fileName;
        Severity = severity;
        Reason = reason;
    }
}

public sealed class ImportReportViewModel : ViewModelBase
{
    public ImportReport Report { get; }
    public string FolderPath { get; }

    public string FolderPathText => string.IsNullOrWhiteSpace(FolderPath)
        ? "Папка: D:\\data\\example"
        : $"Папка: {FolderPath}";

    public string AcceptedBadgeText => $"Принято: {Report.AcceptedCount}";
    public string ErrorsBadgeText => $"Ошибок: {Report.RejectedCount}";
    public string WarningsBadgeText => $"Предупреждений: {Report.WarningCount}";

    public IReadOnlyList<string> FilterOptions { get; } = new[]
    {
        "Показать: все",
        "Показать: только ошибки",
        "Показать: только предупреждения"
    };

    private string _selectedFilter = "Показать: все";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<ImportReportItemViewModel> DisplayItems { get; } = new();
    private readonly List<ImportReportItemViewModel> _allItems = new();

    public ICommand SaveReportCommand { get; }

    public ImportReportViewModel(ImportReport report, string? folderPath = null)
    {
        Report = report;
        FolderPath = folderPath ?? string.Empty;

        // Порядок: сначала ошибки, затем предупреждения, затем успешные
        var sorted = report.Items
            .OrderBy(i => i.Severity switch
            {
                ImportSeverity.Error => 0,
                ImportSeverity.Warning => 1,
                _ => 2
            })
            .ThenBy(i => i.FileName);

        foreach (var item in sorted)
        {
            _allItems.Add(new ImportReportItemViewModel(item.FileName, item.Severity, item.Reason));
        }

        ApplyFilter();

        SaveReportCommand = new RelayCommand(SaveReport);
    }

    private void ApplyFilter()
    {
        DisplayItems.Clear();
        IEnumerable<ImportReportItemViewModel> query = _allItems;

        if (_selectedFilter == "Показать: только ошибки")
        {
            query = _allItems.Where(i => i.Severity == ImportSeverity.Error);
        }
        else if (_selectedFilter == "Показать: только предупреждения")
        {
            query = _allItems.Where(i => i.Severity == ImportSeverity.Warning);
        }

        foreach (var item in query)
        {
            DisplayItems.Add(item);
        }
    }

    private void SaveReport()
    {
        var sfd = new SaveFileDialog
        {
            Filter = "Текстовые файлы (*.txt)|*.txt|CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = "Отчет_об_импорте.txt"
        };

        if (sfd.ShowDialog() == true)
        {
            var lines = new List<string>
            {
                "Отчёт об импорте",
                FolderPathText,
                $"{AcceptedBadgeText} | {ErrorsBadgeText} | {WarningsBadgeText}",
                new string('-', 80),
                string.Format("{0,-35} | {1,-15} | {2}", "Файл", "Результат", "Причина"),
                new string('-', 80)
            };

            foreach (var item in _allItems)
            {
                lines.Add(string.Format("{0,-35} | {1,-15} | {2}", item.FileName, item.SeverityText, item.Reason));
            }

            File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
        }
    }
}

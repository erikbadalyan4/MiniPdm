using System.Windows;
using Microsoft.Win32;
using MiniPdm.Application.Services;
using MiniPdm.Infrastructure.Export;
using MiniPdm.UI.ViewModels;
using MiniPdm.UI.Views;

namespace MiniPdm.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Чистый MVVM: в code-behind нет бизнес-логики, только связь представлений с диалоговыми окнами.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IBomCalculationService _calculationService;
    private readonly IBomDiffService _diffService;
    private readonly ISpecificationExporter _exporter;

    public MainWindow(
        MainViewModel viewModel,
        IBomCalculationService calculationService,
        IBomDiffService diffService,
        ISpecificationExporter exporter)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _calculationService = calculationService;
        _diffService = diffService;
        _exporter = exporter;

        DataContext = _viewModel;

        WireViewActions();

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void WireViewActions()
    {
        _viewModel.SelectFolderAction = () =>
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Выберите папку с CAD-файлами (cad-export)"
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        };

        _viewModel.ShowImportReportAction = (report, folder) =>
        {
            var vm = new ImportReportViewModel(report, folder);
            var win = new ImportReportWindow(vm) { Owner = this };
            win.ShowDialog();
        };

        _viewModel.ShowSpecificationAction = (assemblyId, title) =>
        {
            var vm = new ConsolidatedSpecificationViewModel(assemblyId, title, _calculationService, _exporter);
            var win = new ConsolidatedSpecificationWindow(vm) { Owner = this };
            win.ShowDialog();
        };

        _viewModel.ShowDiffAction = assembly =>
        {
            var vm = new BomDiffViewModel(assembly, _diffService);
            var win = new BomDiffWindow(vm) { Owner = this };
            win.ShowDialog();
        };
    }
}

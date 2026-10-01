using System.Windows;
using MiniPdm.UI.ViewModels;

namespace MiniPdm.UI.Views;

public partial class ImportReportWindow : Window
{
    public ImportReportWindow(ImportReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}


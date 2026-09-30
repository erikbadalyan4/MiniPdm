using System.Windows;
using MiniPdm.UI.ViewModels;

namespace MiniPdm.UI.Views;

public partial class ConsolidatedSpecificationWindow : Window
{
    private readonly ConsolidatedSpecificationViewModel _viewModel;

    public ConsolidatedSpecificationWindow(ConsolidatedSpecificationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.Windows;
using MiniPdm.UI.ViewModels;

namespace MiniPdm.UI.Views;

public partial class BomDiffWindow : Window
{
    public BomDiffWindow(BomDiffViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}


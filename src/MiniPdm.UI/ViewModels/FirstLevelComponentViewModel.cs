namespace MiniPdm.UI.ViewModels;

public sealed class FirstLevelComponentViewModel
{
    public string DesignationDisplay { get; }
    public string Name { get; }
    public int Quantity { get; }
    public string MassDisplay { get; }

    public FirstLevelComponentViewModel(string designationDisplay, string name, int quantity, string massDisplay)
    {
        DesignationDisplay = designationDisplay;
        Name = name;
        Quantity = quantity;
        MassDisplay = massDisplay;
    }
}


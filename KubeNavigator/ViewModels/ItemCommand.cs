using CommunityToolkit.Mvvm.Input;

namespace KubeNavigator.ViewModels;

public class ItemCommand
{
    public required string Name { get; set; }

    public required string Symbol { get; set; }

    public required IRelayCommand Command { get; set; }
}

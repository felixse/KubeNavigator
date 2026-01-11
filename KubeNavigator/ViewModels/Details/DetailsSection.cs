using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace KubeNavigator.ViewModels.Details;

public class DetailsSection : IDetailsSection
{
    public string? Header { get; set; }

    public IRelayCommand? SaveCommand { get; set; }

    public required List<IDetailsItem> Items { get; set; }
}

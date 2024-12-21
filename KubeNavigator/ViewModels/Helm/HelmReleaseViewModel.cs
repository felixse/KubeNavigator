using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Model.Helm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Helm;
public partial class HelmReleaseViewModel : ObservableObject, ISelectable
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public HelmRelease HelmRelease => Revisions.OrderBy(r => r.Version).Last();

    public string Name => HelmRelease.Name;

    public string Namespace => HelmRelease.Namespace;

    public string Chart => HelmRelease.Chart.Metadata.Name;

    public string Revision => HelmRelease.Version.ToString();

    public string Version => HelmRelease.Chart.Metadata.Version;

    public string AppVersion => ""; // todo

    public string Status => HelmRelease.Info.Status;

    public string Updated => HelmRelease.Info.LastDeployed.ToString();

    public ObservableCollection<HelmRelease> Revisions { get; } = [];

    public List<ItemCommand> Commands { get; } = [];

    public HelmReleaseViewModel(HelmRelease helmRelease)
    {
        Revisions.Add(helmRelease);
        Commands.Add(new ItemCommand { Name = "Delete", Symbol = "Dekete", Command = DeleteCommand });
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models.Helm;
using KubeNavigator.Properties;
using KubeNavigator.ViewModels.Details;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Helm;

public partial class HelmReleaseViewModel : ObservableObject, ISelectable, IDetailsSource
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public HelmRelease HelmRelease => Revisions.OrderBy(r => r.Version).Last();

    public string Name => HelmRelease.Name;

    public string Namespace => HelmRelease.Namespace;

    public string Chart => HelmRelease.Chart.Metadata.Name;

    public string Revision => HelmRelease.Version.ToString();

    public string Version => HelmRelease.Chart.Metadata.Version;

    public string AppVersion => HelmRelease.Chart.Metadata.AppVersion;

    public string Status => HelmRelease.Info.Status;

    public string Updated => HelmRelease.Info.LastDeployed.ToString();

    public ObservableCollection<HelmRelease> Revisions { get; } = [];

    public List<ItemCommand> Commands { get; } = [];

    public ClusterViewModel Cluster { get; }

    public string WindowTitle => $"Helm Release: {Name}";

    public string PanelTitle => Name;

    public string PanelSubtitle => "Helm Release";

    public HelmReleaseViewModel(HelmRelease helmRelease, ClusterViewModel cluster)
    {
        Cluster = cluster;
        Revisions.Add(helmRelease);
        Commands.Add(
            new ItemCommand
            {
                Name = "Delete",
                Symbol = "Delete",
                Command = DeleteCommand,
            }
        );
    }

    public event EventHandler? DetailsRefreshRequested;

    [RelayCommand]
    public async Task DeleteAsync() { }

    public async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var values = await Cluster.App.HelmService.GetValuesYamlAsync(HelmRelease);
        return
        [
            new DetailsSection
            {
                Items =
                [
                    new DetailsTextItem { Title = "Chart", Value = Chart },
                    new DetailsTextItem { Title = "Updated", Value = Updated },
                    new DetailsTextItem { Title = "Namespace", Value = Namespace },
                    new DetailsTextItem { Title = "Version", Value = Version },
                    new DetailsTextItem { Title = "Status", Value = Status },
                ],
            },
            new DetailsSection
            {
                Header = "Values",
                Items = [new DetailsEditorItem(string.Empty, values)],
            },
            new DetailsSection
            {
                Header = "Notes",
                Items =
                [
                    new DetailsMarkdownItem
                    {
                        Title = string.Empty,
                        Value = HelmRelease.Info.Notes,
                    },
                ],
            },
        ];
    }
}

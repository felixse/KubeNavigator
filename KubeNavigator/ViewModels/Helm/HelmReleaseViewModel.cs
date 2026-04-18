using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models.Helm;
using KubeNavigator.ViewModels.Details;

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

    public DateTime? Updated => HelmRelease.Info.LastDeployed;

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

    public Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var values = Cluster.App.HelmService.GetValuesYaml(HelmRelease);
        var computedValues = Cluster.App.HelmService.GetComputedValuesYaml(HelmRelease);
        var resources = Cluster.App.HelmService.ParseManifestResources(HelmRelease);

        var resourceRows = resources
            .GroupBy(r => r.Kind)
            .OrderBy(g => g.Key)
            .Select<IGrouping<string, Services.HelmService.ManifestResource>, IDetailsRow>(group =>
            {
                var hasNamespace = group.Any(r => r.Namespace is not null);
                var columns = hasNamespace ? new[] { "Name", "Namespace" } : new[] { "Name" };
                var resourceType = Models.ResourceType.FromKind(group.Key);
                var rows = group
                    .OrderBy(r => r.Name)
                    .Select(r =>
                    {
                        ITableCellContent nameCell = resourceType is not null
                            ? new LinkContent { ResourceName = r.Name, ResourceType = resourceType }
                            : new TextContent { Value = r.Name };

                        return hasNamespace
                            ? (IEnumerable<ITableCellContent>)
                                [nameCell, (TextContent)(r.Namespace ?? "")]
                            : [nameCell];
                    });

                return new FullWidthRow
                {
                    Header = group.Key,
                    Content = new TableContent
                    {
                        Columns = columns,
                        Rows = rows,
                    },
                };
            })
            .ToList();

        return Task.FromResult<List<IDetailsSection>>([
            new DetailsSection
            {
                Rows =
                [
                    new HeaderedRow
                    {
                        Header = "Chart",
                        Content = new TextContent { Value = Chart },
                    },
                    new HeaderedRow
                    {
                        Header = "Updated",
                        Content = new TextContent { Value = Updated?.ToString() },
                    },
                    new HeaderedRow
                    {
                        Header = "Namespace",
                        Content = new TextContent { Value = Namespace },
                    },
                    new HeaderedRow
                    {
                        Header = "Version",
                        Content = new TextContent { Value = Version },
                    },
                    new HeaderedRow
                    {
                        Header = "Status",
                        Content = new TextContent { Value = Status },
                    },
                ],
            },
            new DetailsSection
            {
                Header = "Values",
                Rows =
                [
                    new SegmentedRow
                    {
                        Segments = new Dictionary<string, List<IDetailsRow>>
                        {
                            ["User supplied"] =
                            [
                                new FullWidthRow { Content = new EditorContent { Value = values } },
                            ],
                            ["Computed"] =
                            [
                                new FullWidthRow
                                {
                                    Content = new EditorContent { Value = computedValues },
                                },
                            ],
                        },
                    },
                ],
            },
            new DetailsSection
            {
                Header = "Notes",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new MarkdownContent { Value = HelmRelease.Info.Notes },
                    },
                ],
            },
            new DetailsSection { Header = "Resources", Rows = resourceRows },
        ]);
    }
}

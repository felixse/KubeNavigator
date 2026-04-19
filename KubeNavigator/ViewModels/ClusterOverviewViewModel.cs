using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.ClusterMetrics;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels
{
    public partial class ClusterOverviewViewModel : ObservableObject, INavigationTarget
    {
        private readonly ClusterViewModel _cluster;

        public string Title => "Overview";

        [ObservableProperty]
        public partial KubernetesResourceTypeListViewModel EventListViewModel { get; private set; }

        public List<ClusterMetricBaseViewModel> Metrics { get; } = [];

        public ClusterMetricBaseViewModel CpuMetric { get; }
        public ClusterMetricBaseViewModel MemoryMetric { get; }
        public ClusterMetricBaseViewModel PodMetric { get; }
        public ClusterMetricBaseViewModel NodeMetric { get; }

        public ClusterOverviewViewModel(
            ClusterViewModel cluster,
            KubernetesResourceTypeListViewModel eventListViewModel
        )
        {
            _cluster = cluster;
            EventListViewModel = eventListViewModel;

            CpuMetric = new CpuUsageMetricViewModel();
            MemoryMetric = new MemoryUsageMetricViewModel();
            PodMetric = new PodCountMetricViewModel();
            NodeMetric = new NodeCountMetricViewModel();

            Metrics.Add(CpuMetric);
            Metrics.Add(MemoryMetric);
            Metrics.Add(PodMetric);
            Metrics.Add(NodeMetric);

            _cluster.Context.NodeMetricsUpdated += OnNodeMetricsUpdated;
        }

        private void OnNodeMetricsUpdated(object? sender, NodeMetrics nodeMetrics)
        {
            _cluster.App.DispatcherQueue.TryEnqueue(() =>
            {
                foreach (var metric in Metrics)
                {
                    metric.Update(nodeMetrics);
                }
            });
        }

        public async Task ActivateAsync()
        {
            await EventListViewModel.ActivateAsync();
        }

        public async Task OnNavigatedFrom()
        {
            await EventListViewModel.OnNavigatedFrom();
        }

        public async Task OnNavigatedTo()
        {
            await EventListViewModel.OnNavigatedTo();
        }
    }
}

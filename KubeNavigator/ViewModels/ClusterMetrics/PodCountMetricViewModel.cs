using KubeNavigator.Services;

namespace KubeNavigator.ViewModels.ClusterMetrics
{
    public partial class PodCountMetricViewModel : ClusterMetricBaseViewModel
    {
        public PodCountMetricViewModel()
            : base("Pods") { }

        public override void Update(NodeMetrics nodeMetrics)
        {
            Body = $"{nodeMetrics.TotalPods} / {nodeMetrics.RequestedPods} Requested";
            Percentage =
                nodeMetrics.RequestedPods > 0
                    ? (double)nodeMetrics.TotalPods / nodeMetrics.RequestedPods * 100
                    : 0;
        }
    }
}

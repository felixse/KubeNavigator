using System.Linq;
using KubeNavigator.Models;
using KubeNavigator.Services;

namespace KubeNavigator.ViewModels.ClusterMetrics
{
    public partial class CpuUsageMetricViewModel : ClusterMetricBaseViewModel
    {
        public CpuUsageMetricViewModel()
            : base("CPU Usage") { }

        public override void Update(NodeMetrics nodeMetrics)
        {
            var usedCpu = nodeMetrics.NodeUsage.Values.Aggregate(
                CpuQuantity.Zero,
                (sum, usage) => sum + usage.Cpu
            );
            var totalCpu = nodeMetrics.TotalCpu;

            Body = $"{usedCpu.Format()} / {totalCpu.Format()}";
            Percentage = totalCpu.Nanocores > 0
                ? (double)usedCpu.Nanocores / totalCpu.Nanocores * 100
                : 0;
        }
    }
}

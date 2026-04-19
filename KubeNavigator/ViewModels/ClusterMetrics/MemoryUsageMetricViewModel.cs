using System.Linq;
using KubeNavigator.Models;
using KubeNavigator.Services;

namespace KubeNavigator.ViewModels.ClusterMetrics
{
    public partial class MemoryUsageMetricViewModel : ClusterMetricBaseViewModel
    {
        public MemoryUsageMetricViewModel()
            : base("Memory Usage") { }

        public override void Update(NodeMetrics nodeMetrics)
        {
            var usedMemory = nodeMetrics.NodeUsage.Values.Aggregate(
                MemoryQuantity.Zero,
                (sum, usage) => sum + usage.Memory
            );
            var totalMemory = nodeMetrics.TotalMemory;

            Body = $"{usedMemory.Format()} / {totalMemory.Format()}";
            Percentage = totalMemory.Bytes > 0
                ? (double)usedMemory.Bytes / totalMemory.Bytes * 100
                : 0;
        }
    }
}

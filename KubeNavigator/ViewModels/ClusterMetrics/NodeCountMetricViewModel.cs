using System;
using System.Collections.Generic;
using System.Text;
using KubeNavigator.Services;

namespace KubeNavigator.ViewModels.ClusterMetrics
{
    public partial class NodeCountMetricViewModel : ClusterMetricBaseViewModel
    {
        public NodeCountMetricViewModel()
            : base("Nodes") { }

        public override void Update(NodeMetrics nodeMetrics)
        {
            Body = $"{nodeMetrics.ReadyNodes} / {nodeMetrics.TotalNodes} Ready";
            Percentage = nodeMetrics.TotalNodes > 0
                ? (double)nodeMetrics.ReadyNodes / nodeMetrics.TotalNodes * 100
                : 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Services;

namespace KubeNavigator.ViewModels.ClusterMetrics
{
    public abstract partial class ClusterMetricBaseViewModel : ObservableObject
    {
        public string Title { get; private set; }

        [ObservableProperty]
        public partial string Body { get; protected set; }

        [ObservableProperty]
        public partial double Percentage { get; protected set; }

        public string PercentageText => $"{Percentage:0.#}%";

        partial void OnPercentageChanged(double value)
        {
            OnPropertyChanged(nameof(PercentageText));
        }

        protected ClusterMetricBaseViewModel(string title)
        {
            Title = title;
        }

        public abstract void Update(NodeMetrics nodeMetrics);
    }
}

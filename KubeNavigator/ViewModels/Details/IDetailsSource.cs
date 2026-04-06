using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Details
{
    public interface IDetailsSource
    {
        event EventHandler? DetailsRefreshRequested;

        ClusterViewModel Cluster { get; }

        string WindowTitle { get; }

        string PanelTitle { get; }

        string PanelSubtitle { get; }

        List<ItemCommand> Commands { get; }

        Task<List<IDetailsSection>> CreateDetailsAsync();
    }
}

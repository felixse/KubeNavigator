using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KubeNavigator.Services;

public class ViewState
{
    public List<PinnedItemState> PinnedItems { get; set; } = [];

    public List<string> ExpandedGroups { get; set; } = [];

    public Dictionary<string, ClusterViewState> ClusterStates { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PinnedResourceTypeState), nameof(PinnedResourceTypeState))]
[JsonDerivedType(typeof(PinnedClusterOverviewState), nameof(PinnedClusterOverviewState))]
[JsonDerivedType(typeof(PinnedHelmReleasesState), nameof(PinnedHelmReleasesState))]
public abstract class PinnedItemState { }

public class PinnedResourceTypeState : PinnedItemState
{
    public string Kind { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Plural { get; set; } = string.Empty;
}

public class PinnedClusterOverviewState : PinnedItemState { }

public class PinnedHelmReleasesState : PinnedItemState { }

public class ClusterViewState
{
    public string? LastNamespaceFilter { get; set; }
}

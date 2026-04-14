using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class SegmentedRow : IDetailsRow
{
    public required Dictionary<string, List<IDetailsRow>> Segments { get; set; }
}

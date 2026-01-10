using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class DetailsPortsItem : IDetailsItem
{
    public required List<PortViewModel> Ports { get; set; }
}

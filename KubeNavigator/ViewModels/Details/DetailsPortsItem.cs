using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class DetailsPortsItem : IDetailsItem
{
    public required ICollection<PortViewModel> Ports { get; set; }
}

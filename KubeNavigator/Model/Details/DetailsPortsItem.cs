using System.Collections.Generic;
using KubeNavigator.ViewModels;

namespace KubeNavigator.Model.Details;

public class DetailsPortsItem : IDetailsItem
{
    public required ICollection<PortViewModel> Ports { get; set; }
}

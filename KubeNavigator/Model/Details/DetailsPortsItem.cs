using KubeNavigator.ViewModels;
using System.Collections.Generic;

namespace KubeNavigator.Model.Details;
public class DetailsPortsItem : IDetailsItem
{
    public required ICollection<PortViewModel> Ports { get; set; }
}

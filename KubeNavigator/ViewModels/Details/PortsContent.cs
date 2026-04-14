using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class PortsContent : IDetailsContent
{
    public required List<PortViewModel> Ports { get; set; }
}

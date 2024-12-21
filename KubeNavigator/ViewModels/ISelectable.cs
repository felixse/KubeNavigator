using System.Collections.Generic;

namespace KubeNavigator.ViewModels;
public interface ISelectable
{
    bool IsSelected { get; set; }

    List<ItemCommand> Commands { get; }
}

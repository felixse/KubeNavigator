using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ResourceCreateView : UserControl, IShelfItemView
{
    public ResourceCreateView(CreateKubernetesResourceViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.TextRetriever = () => Editor.Editor.GetText(long.MaxValue);
        this.InitializeComponent();

        var content = ViewModel.GenerateScaffoldYaml();
        Editor.Editor.SetText(content);
    }

    public CreateKubernetesResourceViewModel ViewModel { get; }
}

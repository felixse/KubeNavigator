using CommunityToolkit.WinUI;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace KubeNavigator.Views;

public sealed partial class ResourceEditView : UserControl, IShelfItemView
{
    public ResourceEditView(EditKubernetesResourceViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.TextRetriever = () => Editor.Editor.GetText(long.MaxValue);
        this.InitializeComponent();

        Task.Run(LoadContentAsync);
    }

    public EditKubernetesResourceViewModel ViewModel { get; }

    public async Task LoadContentAsync()
    {
        var content = await ViewModel.LoadResourceBodyAsync();

        await DispatcherQueue.EnqueueAsync(() =>
        {
            Editor.Editor.SetText(content);
            ProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            Editor.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        });
    }
}

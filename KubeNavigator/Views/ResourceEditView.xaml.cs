using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;

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
        try
        {
            var content = await ViewModel.LoadResourceBodyAsync();

            await DispatcherQueue.EnqueueAsync(() =>
            {
                Editor.Editor.SetText(content);
                ProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                Editor.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            });
        }
        catch (System.Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                ViewModel.Resource.Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Error",
                    $"Failed to load {ViewModel.Resource.Resource.Kind} {ViewModel.Resource.Name}: {ex.Message}",
                    NotificationSeverity.Error
                );

                await ViewModel.Resource.Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(ViewModel);
            });
        }
    }
}

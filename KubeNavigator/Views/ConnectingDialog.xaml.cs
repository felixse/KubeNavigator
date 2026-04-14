using System.Threading;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ConnectingDialog : ContentDialog
{
    private readonly CancellationTokenSource _cts = new();

    public string Message { get; }

    public CancellationToken CancellationToken => _cts.Token;

    public ConnectingDialog(string clusterName)
    {
        this.InitializeComponent();
        Message = $"Connecting to cluster \"{clusterName}\"…";
        Closing += OnClosing;
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        _cts.Cancel();
    }
}

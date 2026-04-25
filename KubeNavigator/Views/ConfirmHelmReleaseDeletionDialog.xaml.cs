using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ConfirmHelmReleaseDeletionDialog
    : ContentDialog,
        INotifyPropertyChanged
{
    private readonly CancellationTokenSource _cts = new();
    private string _body = string.Empty;
    private bool _isUninstalling;
    private Func<CancellationToken, Task<string?>>? _uninstallAction;

    public string Body
    {
        get => _body;
        private set
        {
            if (_body != value)
            {
                _body = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Error { get; private set; }

    public ConfirmHelmReleaseDeletionDialog(
        IEnumerable<string> releaseNames,
        string clusterName,
        Func<CancellationToken, Task<string?>> uninstallAction
    )
    {
        this.InitializeComponent();

        _uninstallAction = uninstallAction;

        var names = releaseNames.ToList();
        Title = "Uninstall Helm Release";

        Body = names.Count == 1
            ? $"Uninstall Helm release **{names[0]}** from cluster **{clusterName}**?"
            : $"Uninstall **{names.Count}** Helm releases from cluster **{clusterName}**?\n\n"
              + string.Join("\n", names.Select(n => $"- **{n}**"));

        PrimaryButtonText = "Uninstall";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        Closing += OnClosing;
    }

    private async void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result == ContentDialogResult.Primary && !_isUninstalling)
        {
            args.Cancel = true;
            _isUninstalling = true;

            IsPrimaryButtonEnabled = false;
            PrimaryButtonText = null;
            Body = "Uninstalling\u2026";
            var ring = (ProgressRing)FindName("ProgressIndicator");
            ring.Visibility = Visibility.Visible;

            try
            {
                Error = await _uninstallAction!(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                Error = null;
            }

            Hide();
        }
        else if (_isUninstalling)
        {
            // Allow the programmatic Hide() to close the dialog.
        }
        else
        {
            // User clicked Cancel during the confirmation phase — no-op.
            // During progress phase, cancel the operation.
            _cts.Cancel();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

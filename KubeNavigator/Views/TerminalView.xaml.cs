using CommunityToolkit.WinUI;
using KubeNavigator.Model;
using KubeNavigator.Model.TerminalMessages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

namespace KubeNavigator.Views;

public sealed partial class TerminalView : UserControl
{
    private bool _initialized;

    public TerminalView()
    {
        this.InitializeComponent();
    }

    public event EventHandler? OnInitialized;

    public event EventHandler<string>? OnTextReceived;

    public event EventHandler<TerminalSize>? OnSizeChanged;


    public bool IsReadOnly
    {
        get { return (bool)GetValue(IsReadOnlyProperty); }
        set { SetValue(IsReadOnlyProperty, value); }
    }

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TerminalView), new PropertyMetadata(false));

    public void Write(string text)
    {
        DispatcherQueue.EnqueueAsync(() =>
        {
            var message = new OutputReceived { Data = text };
            WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));
        });
    }

    public void Clear()
    {
        DispatcherQueue.EnqueueAsync(() =>
        {
            var message = new ClearRequested();
            WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));
        });
    }

    public void Close()
    {
        WebView.Close();
    }

    private async void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        var options = new CoreWebView2EnvironmentOptions
        {
            ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay,
        };
        var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);
        await WebView.EnsureCoreWebView2Async(environment);
        WebView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            var message = JsonSerializer.Deserialize<IncomingMessage>(e.WebMessageAsJson);

            if (message is InputReceived input)
            {
                OnTextReceived?.Invoke(this, input.Data);
            }
            else if (message is TerminalSizeChanged size)
            {
                OnSizeChanged?.Invoke(this, new TerminalSize { Height = size.Rows, Width = size.Columns });
            }
        };
        WebView.DefaultBackgroundColor = new Color { A = 0, R = 0, G = 0, B = 0 };
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("webviews.kubenavigator", "WebViews", CoreWebView2HostResourceAccessKind.Allow);
        WebView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
        WebView.CoreWebView2.NavigationCompleted += (s, e) =>
        {
            OnInitialized?.Invoke(this, EventArgs.Empty);
        };
        //WebView.CoreWebView2.OpenDevToolsWindow();
        WebView.CoreWebView2.Navigate("https://webviews.kubenavigator/Terminal/dist/index.html");
    }

    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        args.Handled = true;

        var menu = new MenuFlyout();
        var copy = new MenuFlyoutItem { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy), IsEnabled = args.ContextMenuTarget.HasSelection };
        copy.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.C, Modifiers = VirtualKeyModifiers.Control });
        copy.Click += (s, e) =>
        {
            var data = new DataPackage();
            data.SetText(args.ContextMenuTarget.SelectionText);
            Clipboard.SetContent(data);
        };
        menu.Items.Add(copy);

        var paste = new MenuFlyoutItem { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste), IsEnabled = !IsReadOnly };
        paste.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.V, Modifiers = VirtualKeyModifiers.Control });
        paste.Click += (s, e) =>
        {
            var data = Clipboard.GetContent();
            if (data.Contains(StandardDataFormats.Text))
            {
                data.GetTextAsync().AsTask().ContinueWith(t =>
                {
                    Write(t.Result);
                });
            }
        };
        menu.Items.Add(paste);

        menu.ShowAt(WebView, args.Location);

    }
}

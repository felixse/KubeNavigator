using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;

namespace KubeNavigator.ViewModels.Details;

public partial class SensitiveDataContent : ObservableObject, IDetailsContent
{
    private const string MaskedText = "••••••••••••";

    public SensitiveDataContent(byte[] data)
    {
        IsBinary = !IsValidUtf8Text(data);
        EncodedText = Convert.ToBase64String(data);
        PlainText = IsBinary ? EncodedText : System.Text.Encoding.UTF8.GetString(data);
        DisplayText = MaskedText;
    }

    public bool IsBinary { get; }

    public string PlainText { get; }

    public string EncodedText { get; }

    [ObservableProperty]
    public partial bool IsRevealed { get; set; }

    private string _displayText = string.Empty;

    public string DisplayText
    {
        get => _displayText;
        private set => SetProperty(ref _displayText, value);
    }

    [RelayCommand]
    private void ToggleReveal()
    {
        IsRevealed = !IsRevealed;
        DisplayText = IsRevealed ? PlainText : MaskedText;
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(PlainText);
        Clipboard.SetContent(dataPackage);
    }

    private static bool IsValidUtf8Text(byte[] data)
    {
        // Check for null bytes or other control characters that indicate binary data
        return !data.Any(b => b == 0 || (b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D));
    }
}

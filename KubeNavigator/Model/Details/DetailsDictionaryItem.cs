using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace KubeNavigator.Model.Details;

public class DetailsDictionaryEntry
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public partial class DetailsDictionaryItem : ObservableObject, IDetailsItem
{
    public required string Title { get; init; }
    public required List<DetailsDictionaryEntry> Items { get; init; }

    [RelayCommand]
    public void CopyAsJson()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Items.ToDictionary(x => x.Key, x => x.Value));
        var package = new DataPackage();
        package.SetText(json);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    public void CopyAsYaml()
    {
        var serializer = new YamlDotNet.Serialization.Serializer();
        var yaml = serializer.Serialize(Items.ToDictionary(x => x.Key, x => x.Value));
        var package = new DataPackage();
        package.SetText(yaml);
        Clipboard.SetContent(package);
    }
}

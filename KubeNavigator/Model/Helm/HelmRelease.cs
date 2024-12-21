using k8s.Models;
using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace KubeNavigator.Model.Helm;
public class HelmRelease
{
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public string Name { get; set; }

    public HelmReleaseInformation Info { get; set; }

    public HelmReleaseChart Chart { get; set; }

    public string Manifest { get; set; }

    public int Version { get; set; }

    public string Namespace { get; set; }

    public static HelmRelease FromSecret(V1Secret secret)
    {
        var release = secret.Data["release"];
        var releaseString1 = Encoding.UTF8.GetString(release);
        var length = release.Length;
        var releaseSpan = new Span<byte>(release, 0, length);
        var first = Base64.DecodeFromUtf8InPlace(releaseSpan, out length);
        var releaseString2 = Encoding.UTF8.GetString(release, 0, length);
        var compressedStream = new MemoryStream(release, 0, length);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var releaseData = new MemoryStream();
        gzipStream.CopyTo(releaseData);
        releaseData.Position = 0;
        return JsonSerializer.Deserialize<HelmRelease>(releaseData, _serializerOptions)!;
    }
}

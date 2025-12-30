using k8s.Models;
using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace KubeNavigator.Model.Helm;
public class HelmRelease
{
    public required string Name { get; set; }

    public required HelmReleaseInformation Info { get; set; }

    public required HelmReleaseChart Chart { get; set; }

    public required string Manifest { get; set; }

    public required int Version { get; set; }

    public required string Namespace { get; set; }

    public static HelmRelease FromSecret(V1Secret secret)
    {
        var release = secret.Data["release"];
        var length = release.Length;
        var releaseSpan = new Span<byte>(release, 0, length);
        Base64.DecodeFromUtf8InPlace(releaseSpan, out length);
        var compressedStream = new MemoryStream(release, 0, length);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var releaseData = new MemoryStream();
        gzipStream.CopyTo(releaseData);
        releaseData.Position = 0;
        return JsonSerializer.Deserialize(releaseData, SerializerContext.Default.HelmRelease)!;
    }
}

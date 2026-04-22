using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using k8s.Models;
using KubeNavigator.Models.Helm;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace KubeNavigator.Services;

public partial class HelmService
{
    private readonly ILogger<HelmService> _logger;

    public HelmService(ILogger<HelmService> logger)
    {
        _logger = logger;
    }

    public HelmRelease? ParseReleaseFromSecret(V1Secret secret)
    {
        try
        {
            var secretName = secret.Name();
            var secretNamespace = secret.Namespace();

            Log.ParsingHelmRelease(_logger, secretName, secretNamespace);

            if (!secret.Data.TryGetValue("release", out var releaseData))
            {
                Log.MissingReleaseData(_logger, secretName, secretNamespace);
                return null;
            }

            var length = releaseData.Length;
            var releaseSpan = new Span<byte>(releaseData, 0, length);
            Base64.DecodeFromUtf8InPlace(releaseSpan, out length);

            var compressedStream = new MemoryStream(releaseData, 0, length);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            gzipStream.CopyTo(decompressedStream);
            decompressedStream.Position = 0;

            var release = JsonSerializer.Deserialize(
                decompressedStream,
                SerializerContext.Default.HelmRelease
            );

            if (release != null)
            {
                Log.HelmReleaseParsed(
                    _logger,
                    release.Name,
                    release.Namespace,
                    release.Version,
                    secretName,
                    secretNamespace
                );
            }
            else
            {
                Log.HelmReleaseParseResultNull(_logger, secretName, secretNamespace);
            }

            return release;
        }
        catch (Exception ex)
        {
            Log.ParseHelmReleaseFailed(_logger, secret.Name(), secret.Namespace(), ex);
            return null;
        }
    }

    public string GetValuesYaml(HelmRelease release)
    {
        return JsonElementToYaml(release.Config);
    }

    public string GetComputedValuesYaml(HelmRelease release)
    {
        var chartValues = release.Chart.Values;
        var userConfig = release.Config;

        var chartRawText = chartValues is { ValueKind: JsonValueKind.Object }
            ? chartValues.Value.GetRawText()
            : null;
        var configRawText = userConfig is { ValueKind: JsonValueKind.Object }
            ? userConfig.Value.GetRawText()
            : null;

        // If there are no chart defaults, the computed values are just the user config.
        if (chartRawText is null or "{}")
        {
            return JsonElementToYaml(userConfig);
        }

        // If there is no user config, the computed values are just the chart defaults.
        if (configRawText is null or "{}")
        {
            return JsonElementToYaml(chartValues);
        }

        // Merge chart defaults with user overrides (user wins).
        var deserializer = YamlSerializerFactory.Deserializer;
        var baseDictionary = deserializer.Deserialize<Dictionary<object, object>>(
            new StringReader(chartRawText)
        );
        var overrideDictionary = deserializer.Deserialize<Dictionary<object, object>>(
            new StringReader(configRawText)
        );

        if (baseDictionary is not null && overrideDictionary is not null)
        {
            MergeDictionaries(baseDictionary, overrideDictionary);
        }

        var merged = baseDictionary ?? overrideDictionary;
        if (merged is null)
        {
            return string.Empty;
        }

        return YamlSerializerFactory.Serializer.Serialize(merged);
    }

    public record ManifestResource(string Kind, string Name, string? Namespace);

    public List<ManifestResource> ParseManifestResources(HelmRelease release)
    {
        var resources = new List<ManifestResource>();

        if (string.IsNullOrWhiteSpace(release.Manifest))
        {
            return resources;
        }

        var deserializer = YamlSerializerFactory.Deserializer;
        using var reader = new StringReader(release.Manifest);
        var parser = new YamlDotNet.Core.Parser(reader);
        parser.Consume<YamlDotNet.Core.Events.StreamStart>();

        while (parser.TryConsume<YamlDotNet.Core.Events.DocumentStart>(out _))
        {
            var doc = deserializer.Deserialize<Dictionary<object, object>>(parser);
            parser.TryConsume<YamlDotNet.Core.Events.DocumentEnd>(out _);

            if (doc is null)
            {
                continue;
            }

            var kind = doc.TryGetValue("kind", out var k) ? k?.ToString() : null;
            var name =
                doc.TryGetValue("metadata", out var m)
                && m is Dictionary<object, object> meta
                && meta.TryGetValue("name", out var n)
                    ? n?.ToString()
                    : null;
            var ns =
                m is Dictionary<object, object> meta2
                && meta2.TryGetValue("namespace", out var nsVal)
                    ? nsVal?.ToString()
                    : null;

            if (kind is not null && name is not null)
            {
                resources.Add(new ManifestResource(kind, name, ns));
            }
        }

        return resources;
    }

    private static string JsonElementToYaml(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Object } json || json.GetRawText() is "{}")
        {
            return string.Empty;
        }

        var values = YamlSerializerFactory.Deserializer.Deserialize(new StringReader(json.GetRawText()));
        return YamlSerializerFactory.Serializer.Serialize(values!);
    }

    private static void MergeDictionaries(
        Dictionary<object, object> target,
        Dictionary<object, object> overrides
    )
    {
        foreach (var (key, value) in overrides)
        {
            if (
                value is Dictionary<object, object> overrideChild
                && target.TryGetValue(key, out var existing)
                && existing is Dictionary<object, object> existingChild
            )
            {
                MergeDictionaries(existingChild, overrideChild);
            }
            else
            {
                target[key] = value;
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3001,
            Level = LogLevel.Debug,
            Message = "Parsing Helm release from secret {SecretName} in namespace {Namespace}"
        )]
        public static partial void ParsingHelmRelease(
            ILogger logger,
            string secretName,
            string @namespace
        );

        [LoggerMessage(
            EventId = 3002,
            Level = LogLevel.Information,
            Message = "Parsed Helm release {ReleaseName} v{Version} in namespace {ReleaseNamespace} from secret {SecretName} in namespace {SecretNamespace}"
        )]
        public static partial void HelmReleaseParsed(
            ILogger logger,
            string releaseName,
            string releaseNamespace,
            int version,
            string secretName,
            string secretNamespace
        );

        [LoggerMessage(
            EventId = 3003,
            Level = LogLevel.Error,
            Message = "Failed to parse Helm release from secret {SecretName} in namespace {Namespace}"
        )]
        public static partial void ParseHelmReleaseFailed(
            ILogger logger,
            string secretName,
            string @namespace,
            Exception exception
        );

        [LoggerMessage(
            EventId = 3004,
            Level = LogLevel.Warning,
            Message = "Secret {SecretName} in namespace {Namespace} is missing 'release' data"
        )]
        public static partial void MissingReleaseData(
            ILogger logger,
            string secretName,
            string @namespace
        );

        [LoggerMessage(
            EventId = 3005,
            Level = LogLevel.Warning,
            Message = "Helm release deserialization returned null for secret {SecretName} in namespace {Namespace}"
        )]
        public static partial void HelmReleaseParseResultNull(
            ILogger logger,
            string secretName,
            string @namespace
        );

        [LoggerMessage(
            EventId = 3006,
            Level = LogLevel.Debug,
            Message = "Getting Helm values for release {ReleaseName} in namespace {Namespace} revision {Version}"
        )]
        public static partial void GettingHelmValues(
            ILogger logger,
            string releaseName,
            string @namespace,
            int version
        );

        [LoggerMessage(
            EventId = 3008,
            Level = LogLevel.Debug,
            Message = "Successfully retrieved Helm values for release {ReleaseName} in namespace {Namespace} revision {Version}"
        )]
        public static partial void HelmGetValuesSucceeded(
            ILogger logger,
            string releaseName,
            string @namespace,
            int version
        );
    }
}

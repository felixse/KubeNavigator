using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using k8s.Models;
using KubeNavigator.Model.Helm;
using Microsoft.Extensions.Logging;

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
    }
}

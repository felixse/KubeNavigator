using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using k8s.Models;
using KubeNavigator.Models.Helm;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.Services;

public partial class HelmService
{
    private readonly ILogger<HelmService> _logger;
    private readonly ISettingsService _settingsService;

    public HelmService(ILogger<HelmService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
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

    public async Task<string> GetValuesYamlAsync(
        HelmRelease release,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.GettingHelmValues(_logger, release.Name, release.Namespace, release.Version);

            var helmPath = string.IsNullOrWhiteSpace(_settingsService.Settings.HelmPath)
                ? "helm"
                : _settingsService.Settings.HelmPath;

            var result = await Cli.Wrap(helmPath)
                .WithArguments(args =>
                {
                    args.Add("get");
                    args.Add("values");
                    args.Add(release.Name);
                    args.Add("--namespace");
                    args.Add(release.Namespace);
                    args.Add("--revision");
                    args.Add(release.Version.ToString());
                    args.Add("--output");
                    args.Add("yaml");
                })
                .ExecuteBufferedAsync(cancellationToken);

            Log.HelmGetValuesSucceeded(_logger, release.Name, release.Namespace, release.Version);
            return result.StandardOutput;
        }
        catch (Exception ex)
        {
            Log.HelmGetValuesFailed(_logger, release.Name, release.Namespace, release.Version, ex);
            throw;
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
            EventId = 3007,
            Level = LogLevel.Error,
            Message = "helm get values failed for release {ReleaseName} in namespace {Namespace} revision {Version}"
        )]
        public static partial void HelmGetValuesFailed(
            ILogger logger,
            string releaseName,
            string @namespace,
            int version,
            Exception exception
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

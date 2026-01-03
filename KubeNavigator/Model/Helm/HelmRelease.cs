using System;
using System.Text.Json;
using k8s.Models;

namespace KubeNavigator.Model.Helm;

public class HelmRelease
{
    public required string Name { get; set; }

    public required HelmReleaseInformation Info { get; set; }

    public required HelmReleaseChart Chart { get; set; }

    public required string Manifest { get; set; }

    public required int Version { get; set; }

    public required string Namespace { get; set; }
}

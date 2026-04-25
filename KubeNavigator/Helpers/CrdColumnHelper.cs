using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.Helpers;

public static class CrdColumnHelper
{
    /// <summary>
    /// Extracts the additional printer columns from the stored/served CRD version,
    /// filtering out the "age" column and columns with priority > 0 (detail-only),
    /// mirroring the Freelens approach.
    /// </summary>
    public static ImmutableArray<CrdPrinterColumn> GetPrinterColumns(
        V1CustomResourceDefinition crd
    )
    {
        var version = crd.Spec.Versions?.FirstOrDefault(v => v.Storage) ?? crd.Spec.Versions?.FirstOrDefault();

        if (version?.AdditionalPrinterColumns is not { Count: > 0 } columns)
        {
            return [];
        }

        return
        [
            .. columns
                .Where(c =>
                    !string.Equals(c.Name, "age", StringComparison.OrdinalIgnoreCase)
                    && c.Priority is null or 0
                )
                .Select(c => new CrdPrinterColumn(c.Name, c.JsonPath, c.Type)),
        ];
    }

    /// <summary>
    /// Builds <see cref="ResourceColumn"/> instances for the given CRD printer columns.
    /// Each column resolves its value at runtime via simple JSONPath evaluation on the
    /// resource's extension data.
    /// </summary>
    public static ImmutableArray<ResourceColumn> BuildColumns(
        ResourceType resourceType
    )
    {
        var builder = ImmutableArray.CreateBuilder<ResourceColumn>();

        builder.Add(new ResourceColumn("Name", vm => vm.Name, PropertyName: nameof(KubernetesResourceViewModel.Name)));

        if (resourceType.IsNamespaceScoped)
        {
            builder.Add(new ResourceColumn("Namespace", vm => vm.Namespace, PropertyName: nameof(KubernetesResourceViewModel.Namespace)));
        }

        if (!resourceType.AdditionalColumns.IsDefaultOrEmpty)
        {
            foreach (var col in resourceType.AdditionalColumns)
            {
                var jsonPath = col.JsonPath;
                builder.Add(new ResourceColumn(
                    col.Name,
                    vm => ResolveJsonPath(vm.Resource, jsonPath)
                ));
            }
        }

        builder.Add(new ResourceColumn("Age", vm => vm.Age, ResourceColumnType.Age, nameof(KubernetesResourceViewModel.Age)));

        return builder.ToImmutable();
    }

    /// <summary>
    /// Resolves a simple JSONPath expression (e.g. <c>.spec.replicas</c> or
    /// <c>.status.conditions[0].type</c>) against a Kubernetes resource.
    /// Supports dot-notation with optional array indexing.
    /// </summary>
    internal static string? ResolveJsonPath(
        IKubernetesObject<V1ObjectMeta> resource,
        string jsonPath
    )
    {
        if (resource is not GenericKubernetesObject generic || generic.ExtensionData is null)
        {
            return null;
        }

        // JSONPath from CRD starts with a leading dot, e.g. ".spec.replicas"
        var path = jsonPath.TrimStart('.');
        var segments = ParseSegments(path);

        JsonElement? current = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var (name, index) = segments[i];

            if (i == 0)
            {
                // First segment: look up in extension data dictionary
                if (!generic.ExtensionData.TryGetValue(name, out var root))
                {
                    return null;
                }
                current = root;
            }
            else
            {
                if (current is not { ValueKind: JsonValueKind.Object } obj)
                {
                    return null;
                }

                if (!obj.TryGetProperty(name, out var next))
                {
                    return null;
                }
                current = next;
            }

            // Handle array index
            if (index.HasValue)
            {
                if (current is not { ValueKind: JsonValueKind.Array } arr)
                {
                    return null;
                }
                var idx = index.Value;
                if (idx < 0 || idx >= arr.GetArrayLength())
                {
                    return null;
                }
                current = arr[idx];
            }
        }

        return current switch
        {
            null => null,
            { ValueKind: JsonValueKind.String } v => v.GetString(),
            { ValueKind: JsonValueKind.Number } v => v.GetRawText(),
            { ValueKind: JsonValueKind.True } => "True",
            { ValueKind: JsonValueKind.False } => "False",
            { ValueKind: JsonValueKind.Null } => null,
            var v => v.Value.GetRawText(),
        };
    }

    private static List<(string Name, int? Index)> ParseSegments(string path)
    {
        var result = new List<(string, int?)>();
        foreach (var segment in path.Split('.'))
        {
            var bracketIdx = segment.IndexOf('[');
            if (bracketIdx >= 0)
            {
                var name = segment[..bracketIdx];
                var idxStr = segment[(bracketIdx + 1)..segment.IndexOf(']')];
                result.Add((name, int.Parse(idxStr)));
            }
            else
            {
                result.Add((segment, null));
            }
        }
        return result;
    }
}

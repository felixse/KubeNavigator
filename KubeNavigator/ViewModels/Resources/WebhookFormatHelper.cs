using System.Collections.Generic;
using System.Linq;
using k8s.Models;

namespace KubeNavigator.ViewModels.Resources;

internal static class WebhookFormatHelper
{
    public static string FormatClientConfig(Admissionregistrationv1WebhookClientConfig? config)
    {
        if (config is null)
            return string.Empty;

        if (config.Service is not null)
        {
            var parts = new List<string>();
            parts.Add($"Name: {config.Service.Name}");
            parts.Add($"Namespace: {config.Service.NamespaceProperty}");
            if (config.Service.Port.HasValue)
                parts.Add($"Port: {config.Service.Port}");
            if (!string.IsNullOrEmpty(config.Service.Path))
                parts.Add($"Path: {config.Service.Path}");
            return string.Join("\n", parts);
        }

        return config.Url ?? string.Empty;
    }

    public static string FormatLabelSelector(V1LabelSelector? selector)
    {
        if (selector is null)
            return string.Empty;

        var lines = new List<string>();

        lines.Add($"Match Expressions: {(selector.MatchExpressions is { Count: > 0 }
            ? string.Join(", ", selector.MatchExpressions.Select(e =>
                $"{e.Key} {e.OperatorProperty} ({string.Join(", ", e.Values ?? [])})"))
            : string.Empty)}");

        if (selector.MatchLabels is { Count: > 0 })
        {
            lines.Add($"Match Labels:");
            foreach (var l in selector.MatchLabels)
            {
                lines.Add($"  {l.Key}={l.Value}");
            }
        }

        return string.Join("\n", lines);
    }

    public static string FormatRules(IList<V1RuleWithOperations>? rules)
    {
        if (rules is null || rules.Count == 0)
            return string.Empty;

        var allLines = new List<string>();
        foreach (var r in rules)
        {
            var groups = string.Join(", ", r.ApiGroups?.Select(g => string.IsNullOrEmpty(g) ? "*" : g) ?? []);
            allLines.Add($"API Groups: {groups}");
            allLines.Add($"API Versions: {string.Join(", ", r.ApiVersions ?? [])}");
            allLines.Add($"Operations: {string.Join(", ", r.Operations ?? [])}");
            allLines.Add($"Resources: {string.Join(", ", r.Resources ?? [])}");
            if (r.Scope is not null)
                allLines.Add($"Scope: {r.Scope}");
        }

        return string.Join("\n", allLines);
    }
}

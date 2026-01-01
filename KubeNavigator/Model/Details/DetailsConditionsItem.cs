using System;
using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public enum ConditionKind
{
    Positive,
    Negative,
}

internal class DetailsConditionsElement
{
    public required string Type { get; set; }
    public required string Status { get; set; }
    public DateTime? LastTransitionTime { get; set; }
    public DateTime? LastHeartbeatTime { get; set; }
    public string? Reason { get; set; }
    public string? Message { get; set; }

    public ConditionKind Kind =>
        Type switch
        {
            "Ready" => ConditionKind.Positive,
            "ContainersReady" => ConditionKind.Positive,
            "PodScheduled" => ConditionKind.Positive,
            "Initialized" => ConditionKind.Positive,
            "PodReadyToStartContainers" => ConditionKind.Positive,
            "MemoryPressure" => ConditionKind.Negative,
            "DiskPressure" => ConditionKind.Negative,
            "PIDPressure" => ConditionKind.Negative,
            _ => ConditionKind.Negative,
        };

    public string KindString => Kind.ToString();

    /// <summary>
    /// multi-line details text combining Reason and Message
    /// </summary>
    public string DetailsText =>
        $"Last Transition Time: {LastTransitionTime}\r\nStatus: {Status}\r\nReason: {Reason}\r\nMessage: {Message}";
}

internal class DetailsConditionsItem : IDetailsItem
{
    public required string Title { get; set; }

    public required List<DetailsConditionsElement> Items { get; set; }
}

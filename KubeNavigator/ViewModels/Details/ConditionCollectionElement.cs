using System;

namespace KubeNavigator.ViewModels.Details;

public enum ConditionKind
{
    Positive,
    Negative,
    Neutral,
}

public class ConditionCollectionElement : IDetailsCollectionElement
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
            "Available" => ConditionKind.Positive,
            "Progressing" => ConditionKind.Neutral,
            _ => ConditionKind.Neutral,
        };

    public string KindString => Kind.ToString();

    public string DetailsText =>
        $"Last Transition Time: {LastTransitionTime}\r\nStatus: {Status}\r\nReason: {Reason}\r\nMessage: {Message}";
}

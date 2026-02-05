namespace IntuneLight.Models.Defender;

// Root response from Defender MachineActions endpoint.
public sealed class DefenderMachineActions
{
    // OData metadata context.
    public string? OdataContext { get; set; }

    // Collection of machine actions.
    public List<DefenderMachineAction> Value { get; set; } = [];
}

// Represents a Defender machine action (e.g. Isolate).
public sealed class DefenderMachineAction
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RequestorComment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string ComputerDnsName { get; set; } = string.Empty;
    public DateTime CreationDateTimeUtc { get; set; }
    public DateTime? LastUpdateDateTimeUtc { get; set; }
    public DateTime? CancellationDateTimeUtc { get; set; }
}

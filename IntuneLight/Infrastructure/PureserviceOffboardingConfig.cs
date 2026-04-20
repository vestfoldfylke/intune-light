namespace IntuneLight.Infrastructure;

// Pureservice configuration values for offboarding tickets.
public class PureserviceOffboardingOptions
{
    // Optional: defaults are provided as starting values, but may need to be overridden per environment or Pureservice tenant.
    public int TicketTypeId { get; set; } = 4;
    public int PriorityId { get; set; } = 4;
    public int StatusId { get; set; } = 7;
    public int SourceId { get; set; } = 1;
    public int TeamId { get; set; } = 3;
    public int Category1Id { get; set; } = 17;
    public int Category2Id { get; set; } = 255;
    public int RequestTypeId { get; set; } = 1;
    public int RelationshipTypeId { get; set; } = 61;
    public int AssetStatusSoldId { get; set; } = 29;

    // Required: no defaults as these values differ between environments and Pureservice tenants.
    public required int DepartmentId { get; set; }
    public required int AgentId { get; set; }
}
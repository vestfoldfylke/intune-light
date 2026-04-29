namespace IntuneLight.Infrastructure;

// PureService configuration for offboarding ticket creation.
// All ticket type, category and priority values are derived automatically from the existing
// PureService case linked to the asset. Only the fallback service agent and department must be configured.
public class PureserviceOffboardingOptions
{
    // ID of the dedicated service agent used as fallback assignedAgent on auto-resolved offboarding tickets.
    public required int AgentId { get; set; }

    // PureService company ID used as assignedDepartment on offboarding tickets.
    // Note: Despite the name, PureService expects companyId here, not companyDepartmentId.
    public required int DepartmentId { get; set; }
}
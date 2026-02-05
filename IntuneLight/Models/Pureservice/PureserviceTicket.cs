namespace IntuneLight.Models.Pureservice;

// Search response for Pureservice ticket.
public sealed class PureserviceTicketSearchResponse
{
    public List<PureserviceTicket> Tickets { get; set; } = [];
}

// Represents a Pureservice ticket with the most important fields for lookup and diagnostics.
public sealed class PureserviceTicket
{
    public string? Solution { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public int? ReopenedCount { get; set; }
    public DateTime? Responded { get; set; }
    public DateTime? Resolved { get; set; }
    public DateTime? Reopened { get; set; }
    public DateTime? Closed { get; set; }
    public DateTime UserWaitedSince { get; set; }
    public DateTime? UserReminded { get; set; }
    public DateTime? PendingUserSince { get; set; }
    public bool IsMarkedForDeletion { get; set; }
    public int TotalTimelogMinutes { get; set; }
    public int AssignedDepartmentId { get; set; }
    public int? PriorityId { get; set; }
    public int TicketTypeId { get; set; }
    public int UserId { get; set; }
    public int RequestNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public int SourceId { get; set; }
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public int CreatedById { get; set; }
    public int ModifiedById { get; set; }

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = string.Empty;
}
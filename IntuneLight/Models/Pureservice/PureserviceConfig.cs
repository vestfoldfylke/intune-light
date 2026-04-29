namespace IntuneLight.Models.Pureservice;

// Root response for ticket statuses.
public sealed class PureserviceTicketStatusSearchResponse
{
    public List<PureserviceTicketStatus> Statuses { get; set; } = [];
}

// Represents a PureService ticket status.
public sealed class PureserviceTicketStatus
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Disabled { get; set; }
}

// Root response for ticket types.
public sealed class PureserviceTicketTypeSearchResponse
{
    public List<PureserviceTicketType> TicketTypes { get; set; } = [];
}

// Represents a PureService ticket type.
public sealed class PureserviceTicketType
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Disabled { get; set; }
}

// Root response for priorities.
public sealed class PureservicePrioritySearchResponse
{
    public List<PureservicePriority> Priorities { get; set; } = [];
}

// Represents a PureService priority.
public sealed class PureservicePriority
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Disabled { get; set; }
    public int? RequestTypeId { get; set; }
}

// Root response for sources.
public sealed class PureserviceSourceSearchResponse
{
    public List<PureserviceSource> Sources { get; set; } = [];
}

// Represents a PureService source.
public sealed class PureserviceSource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Disabled { get; set; }
}

// Root response for request types.
public sealed class PureserviceRequestTypeSearchResponse
{
    public List<PureserviceRequestType> RequestTypes { get; set; } = [];
}

// Represents a PureService request type.
public sealed class PureserviceRequestType
{
    public int Id { get; set; }
    public string? Key { get; set; }
    public bool Disabled { get; set; }
}

// Root response for categories.
public sealed class PureserviceCategorySearchResponse
{
    public List<PureserviceCategory> Categories { get; set; } = [];
}

// Represents a PureService category.
public sealed class PureserviceCategory
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Disabled { get; set; }
}

// Root response for relationship types.
public sealed class PureserviceRelationshipTypeSearchResponse
{
    public List<PureserviceRelationshipType> RelationshipTypes { get; set; } = [];
}

// Root response for asset type with statuses.
public sealed class PureserviceAssetTypeResponse
{
    public PureserviceAssetTypeLinked? Linked { get; set; }
}

public sealed class PureserviceAssetTypeLinked
{
    public List<PureserviceAssetStatus> AssetStatuses { get; set; } = [];
}

// Represents a PureService asset status.
public sealed class PureserviceAssetStatus
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int CoreStatus { get; set; }
    public bool Disabled { get; set; }
    public int AssetTypeId { get; set; }
}
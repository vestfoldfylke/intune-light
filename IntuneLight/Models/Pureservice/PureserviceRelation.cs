namespace IntuneLight.Models.Pureservice;

// Root response for "Relationships - List relationships to asset".
public sealed class PureserviceRelationshipSearchResponse
{
    public List<PureserviceRelationship> Relationships { get; set; } = [];

    public PureserviceRelationshipLinked Linked { get; set; } = new();

    // Raw JSON representation
    public string RawJson { get; set; } = string.Empty;
}

// Container for linked entities in the relationship response.
public sealed class PureserviceRelationshipLinked
{
    public List<PureserviceRelationshipType> RelationshipTypes { get; set; } = [];

    public List<PureserviceTicket> Tickets { get; set; } = [];

    public List<PureserviceUser> Users { get; set; } = [];
}

// A single relationship row between an asset and another entity (ticket, user, company, etc.).
public sealed class PureserviceRelationship
{
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string? Main { get; set; }
    public string? InverseMain { get; set; }
    public bool SolvingRelationship { get; set; }
    public int? ToTicketId { get; set; }
    public int? FromTicketId { get; set; }
    public int? ToChangeId { get; set; }
    public int? FromChangeId { get; set; }
    public int? ToAssetId { get; set; }
    public int? FromAssetId { get; set; }
    public int? ToUserId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToCompanyId { get; set; }
    public int? FromCompanyId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public int CreatedById { get; set; }
    public int ModifiedById { get; set; }
    public PureserviceRelationshipLinks Links { get; set; } = new();
}

// Link references for a relationship (type, ticket, asset, user, etc.).
public sealed class PureserviceRelationshipLinks
{
    public PureserviceLinkRef? Type { get; set; }
    public PureserviceLinkRef? ImportJob { get; set; }
    public PureserviceLinkRef? ToTicket { get; set; }
    public PureserviceLinkRef? FromTicket { get; set; }
    public PureserviceLinkRef? ToChange { get; set; }
    public PureserviceLinkRef? FromChange { get; set; }
    public PureserviceLinkRef? ToAsset { get; set; }
    public PureserviceLinkRef? FromAsset { get; set; }
    public PureserviceLinkRef? ToUser { get; set; }
    public PureserviceLinkRef? FromUser { get; set; }
    public PureserviceLinkRef? ToCompany { get; set; }
    public PureserviceLinkRef? FromCompany { get; set; }
    public PureserviceLinkRef? CreatedBy { get; set; }
    public PureserviceLinkRef? ModifiedBy { get; set; }
}

// Reference to another entity in Pureservice (id + type).
public sealed class PureserviceLinkRef
{
    public int Id { get; set; }
    public string? Type { get; set; }
}

// Relationship type (e.g. "Utlevert").
public sealed class PureserviceRelationshipType
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ReverseName { get; set; }
    public bool Disabled { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public int? FromAssetTypeId { get; set; }
    public int? ToAssetTypeId { get; set; }
    public int? FromTicketTypeId { get; set; }
    public int? ToTicketTypeId { get; set; }
    public int? FromChangeTypeId { get; set; }
    public int? ToChangeTypeId { get; set; }
    public int? RelationshipTypeGroupId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public int CreatedById { get; set; }
    public int ModifiedById { get; set; }
}
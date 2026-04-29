using IntuneLight.Models.Pureservice;

namespace IntuneLight.Services;

// Caches static PureService configuration data that does not change between searches.
public sealed class PureserviceConfigCache
{
    public List<PureserviceTicketStatus> TicketStatuses { get; set; } = [];
    public List<PureserviceTicketType> TicketTypes { get; set; } = [];
    public List<PureservicePriority> Priorities { get; set; } = [];
    public List<PureserviceSource> Sources { get; set; } = [];
    public List<PureserviceRequestType> RequestTypes { get; set; } = [];
    public List<PureserviceCategory> Categories { get; set; } = [];
    public List<PureserviceRelationshipType> RelationshipTypes { get; set; } = [];
}
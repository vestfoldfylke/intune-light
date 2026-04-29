namespace IntuneLight.Models.Pureservice;

// Well-known PureService name constants used for dynamic lookup.
// Note: If your PureService environment uses different names, update these constants accordingly.
public static class PureserviceNames
{
    // Ticket statuses
    public const string TicketStatusResolved = "Løst";

    // Asset statuses
    public const string AssetStatusSold = "Solgt";
    public const string AssetStatusDiscarded = "Kassert (DRIG)";
    public const string AssetStatusShared = "Dele-pc";
    public const string AssetStatusLost = "Tapt";
    public const string AssetStatusStolen = "Stjålet";
    public const string AssetStatusDiscardedRedistributed = "Kassert (Omdisponert i fylkeskommunen)";

    // Ticket configuration
    public const string TicketTypeName = "Forespørsel";
    public const string PriorityName = "Normal";
    public const string SourceName = "Direkte";
    public const string Category1Name = "Maskinvare";
    public const string Category2Name = "PC";
    public const string Category3Name = "Privatisering";

    // Relationship types
    public const string RequestTypeName = "Ticket";
    public const string RelationshipTypePrivatizationEmployee = "Privatisering av ansatt pc";
    public const string RelationshipTypePrivatizationStudent = "Privatisering av elev pc";
    public const string RelationshipTypeDeletion = "Kassert";
}
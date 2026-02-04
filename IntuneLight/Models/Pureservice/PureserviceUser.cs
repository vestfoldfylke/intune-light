namespace IntuneLight.Models.Pureservice;

// Search response for Pureservice users.
public sealed class PureserviceUserSearchResponse
{
    public List<PureserviceUser> Users { get; set; } = [];
}

// Minimal user model from Pureservice for lookup.
public sealed class PureserviceUser
{
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsSuperuser { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = "";

}

namespace IntuneLight.Models.Pureservice;

// Search response for Pureservice assets.
public sealed class PureserviceAssetSearchResponse
{
    public List<PureserviceAsset> Assets { get; set; } = [];
}

// Represents a Pureservice asset with core metadata for lookup and diagnostics.
public sealed class PureserviceAsset
{
    public string Name { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public bool IsMarkedForDeletion { get; set; }
    public int TypeId { get; set; }
    public int StatusId { get; set; }

    public int? ImportedById { get; set; }
    public int? ImportJobId { get; set; }

    public int Id { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
    public int CreatedById { get; set; }
    public int? ModifiedById { get; set; }

    // Raw JSON payload for debugging / raw viewer
    public string RawJson { get; set; } = "";
}
using System.Text.Json.Serialization;

namespace IntuneLight.Models.Entra;

public sealed class EntraDeviceCount
{
    [JsonPropertyName("@odata.context")]
    public string? ODataContext { get; set; }

    public List<object> Value { get; set; } = [];

    // Returns the number of directory objects in the response.
    public int Count => Value.Count;

    // Holds the raw JSON payload for debugging / raw view dialog.
    public string RawJson { get; set; } = string.Empty;
}



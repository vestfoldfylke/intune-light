using System.Text.Json.Serialization;

namespace IntuneLight.Models.Entra;

public sealed class EntraDeviceCount
{
    [JsonPropertyName("@odata.context")]
    public string? ODataContext { get; set; }

    [JsonPropertyName("value")]
    public List<object> Value { get; set; } = [];

    // Returns the number of directory objects in the response.
    public int Count => Value.Count;
}

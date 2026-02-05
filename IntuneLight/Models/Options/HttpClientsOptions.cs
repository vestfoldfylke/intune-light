namespace IntuneLight.Models.Options;

public sealed class HttpClientsOptions
{
    public HttpClientSettings Graph { get; set; } = new();
    public HttpClientSettings Defender { get; set; } = new();
}

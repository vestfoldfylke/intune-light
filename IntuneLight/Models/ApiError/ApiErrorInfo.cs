namespace IntuneLight.Models.ApiError;

// Represents detailed information about an API error response.
public sealed class ApiErrorInfo
{
    public string SystemName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
}

// Exception that encapsulates API error information.
public sealed class ApiException(ApiErrorInfo info) : Exception($"{info.SystemName} returned {info.StatusCode} {info.ReasonPhrase}")
{
    public ApiErrorInfo ErrorInfo { get; } = info;
}
using IntuneLight.Models.ApiError;

namespace IntuneLight.Infrastructure;

public interface IApiResponseGuard
{
    bool EnsureBinaryBody(byte[] content, string systemName, string url, int statusCode);
    bool EnsureJsonBody(string body, string systemName, string url, int statusCode);
    void EnsureSuccess(HttpResponseMessage response, string systemName, string url, string body);
}

// Guards API responses and throws structured exceptions on failure.
public class ApiResponseGuard(ILogger<ApiResponseGuard> logger) : IApiResponseGuard
{
    private readonly ILogger<ApiResponseGuard> _logger = logger;

    // Throws ApiException with structured metadata if response is not successful.
    public void EnsureSuccess(HttpResponseMessage response, string systemName, string url, string body)
    {
        // If the response indicates success, simply return
        if (response.IsSuccessStatusCode)
            return;

        // Create ApiErrorInfo with relevant details
        var info = new ApiErrorInfo
        {
            SystemName = systemName,
            Url = url,
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? string.Empty,
            ResponseBody = body
        };

        // Log structured error for central logging
        _logger.LogError(
            "API call to {System} failed with status {StatusCode} {Reason}. Url: {Url}. Body: {Body}",
            info.SystemName,
            info.StatusCode,
            info.ReasonPhrase,
            info.Url,
            info.ResponseBody);

        throw new ApiException(info);
    }

    // Ensures the response body contains JSON when the call succeeded.
    // Returns false when the body is empty (no data), without throwing.
    public bool EnsureJsonBody(string body, string systemName, string url, int statusCode)
    {
        // Body exists → OK
        if (!string.IsNullOrWhiteSpace(body))
            return true;

        _logger.LogInformation(
            "API call to {System} returned success but empty body. Status {StatusCode}. Url: {Url}.",
            systemName,
            statusCode,
            url);

        return false;
    }


    // Returns false when a successful response contains no binary content.
    public bool EnsureBinaryBody(byte[] content, string systemName, string url, int statusCode)
    {
        // Content exists → OK
        if (content is { Length: > 0 })
            return true;

        _logger.LogInformation(
            "API call to {System} returned success but empty binary body. Status {StatusCode}. Url: {Url}.",
            systemName,
            statusCode,
            url);

        return false;
    }
}

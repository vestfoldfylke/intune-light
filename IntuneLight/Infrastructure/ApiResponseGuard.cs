using IntuneLight.Models.ApiError;

namespace IntuneLight.Infrastructure;

public interface IApiResponseGuard
{
    bool EnsureBinaryBody(byte[] content, string systemName, string url, int statusCode);
    bool EnsureJsonBody(string body, string systemName, string url, int statusCode);
    void EnsureSuccess(HttpResponseMessage response, string systemName, string url, string body);
    bool EnsureSuccessOrNoData(HttpResponseMessage response, string systemName, string url, string body);
}

// Guards API responses and throws structured exceptions on failure.
public class ApiResponseGuard(ILogger<ApiResponseGuard> logger) : IApiResponseGuard
{
    private readonly ILogger<ApiResponseGuard> _logger = logger;

    // Throws an ApiException for non-success HTTP responses (4xx/5xx).
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

    // Ensures the response is successful or represents valid "no data".
    public bool EnsureSuccessOrNoData(HttpResponseMessage response, string systemName, string url, string body)
    {
        // Treat these as "no data" in search/enrichment flows
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _logger.LogInformation(
                "API call to {System} returned no data. Status {StatusCode}. Url: {Url}.",
                systemName,
                (int)response.StatusCode,
                url);

            return false; // Caller should return null without UI noise
        }

        // All other non-success are real errors
        EnsureSuccess(response, systemName, url, body); // Throws ApiException
        return true;
    }

    /// Ensures a successful response contains a JSON body.
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

    // Ensures a successful response contains binary content.
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

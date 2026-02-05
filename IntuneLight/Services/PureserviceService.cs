using System.Net.Http.Headers;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Pureservice;

namespace IntuneLight.Services;

public interface IPureserviceService
{
    Task<PureserviceUser?> GetUserByEmailAsync(string email);
    Task<PureserviceTicket?> GetTicketByRequestNumberAsync(string requestNumber);
    Task<PureserviceAsset?> GetAssetBySerialAsync(string serialNumber);
    Task<PureserviceRelationshipSearchResponse?> GetRelationshipsByAssetIdAsync(string assetId);
}

public sealed class PureserviceService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IApiResponseGuard guard) : IPureserviceService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Looks up a Pureservice user by primary email address.
    public async Task<PureserviceUser?> GetUserByEmailAsync(string email)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email kan ikke være null eller en tom string.", nameof(email));

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL with filter for UPN
        var filter = $"disabled == false && emailAddress.Email == \"{email}\"";
        var encodedFilter = Uri.EscapeDataString(filter);
        var url = $"user/?filter={encodedFilter}";

        // Send GET request to Pureservice API
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Pureservice | User", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Pureservice | User", url, (int)response.StatusCode))
            return null;

        // Deserialize response and return first user found
        var payload = JsonSerializer.Deserialize<PureserviceUserSearchResponse>(content, _jsonSerializerOptions);
        var pureserviceUser = payload?.Users?.FirstOrDefault();

        // Attach raw json
        if (pureserviceUser != null)
            pureserviceUser.RawJson = content;

        return pureserviceUser;
    }

    // Looks up a Pureservice ticket by ticket id.
    public async Task<PureserviceTicket?> GetTicketByRequestNumberAsync(string requestNumber)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(requestNumber))
            throw new ArgumentException("Ticket nummer kan ikke være null eller en tom string.", nameof(requestNumber));

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL with filter for ticket-id
        var encodedNumber = Uri.EscapeDataString(requestNumber);
        var url = $"ticket/{encodedNumber}/requestNumber/";

        // Send GET request to Pureservice API
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Pureservice | Ticket", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Pureservice | Ticket", url, (int)response.StatusCode))
            return null;

        // Deserialize response and return ticket
        var payload = JsonSerializer.Deserialize<PureserviceTicketSearchResponse>(content, _jsonSerializerOptions);
        var pureserviceTicket = payload?.Tickets?.FirstOrDefault();

        // Attach raw json
        if (pureserviceTicket != null)
            pureserviceTicket.RawJson = content;

        return pureserviceTicket;
    }

    // Looks up a Pureservice asset by serial number using the asset endpoint. (minimal response)
    public async Task<PureserviceAsset?> GetAssetBySerialAsync(string serialNumber)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serienummer kan ikke være null eller en tom string.", nameof(serialNumber));

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL with filter for serial number
        const int limit = 5;
        var encodedQuery = Uri.EscapeDataString(serialNumber);
        var searchUrl = $"asset/search?query={encodedQuery}&limit={limit}";

        // Search asset by serial number(minimal response)
        var searchResponse = await client.GetAsync(searchUrl);
        var searchContent = await searchResponse.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(searchResponse, "Pureservice | Asset", searchUrl, searchContent);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(searchContent, "Pureservice | Asset", searchUrl, (int)searchResponse.StatusCode))
            return null;

        // Deserialize search response
        var searchPayload = JsonSerializer.Deserialize<PureserviceAssetSearchResponse>(
            searchContent, _jsonSerializerOptions);

        // If no assets found, return null
        var searchAsset = searchPayload?.Assets?.FirstOrDefault();
        if (searchAsset == null || string.IsNullOrWhiteSpace(searchAsset.Id.ToString()))
            return null;

        // Fetch full asset details by asset ID
        var encodedAssetId = Uri.EscapeDataString(searchAsset.Id.ToString());
        var assetUrl = $"asset/{encodedAssetId}";

        // Find asset by ID (full response)
        var assetResponse = await client.GetAsync(assetUrl);
        var assetContent = await assetResponse.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(assetResponse, "Pureservice | Asset", assetUrl, assetContent);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(assetContent, "Pureservice | Asset", assetUrl, (int)assetResponse.StatusCode))
            return null;

        // Deserialize full asset response
        var assetPayload = JsonSerializer.Deserialize<PureserviceAssetSearchResponse>(
            assetContent, _jsonSerializerOptions);

        // If no assets found, return null
        var asset = assetPayload?.Assets?.FirstOrDefault();
        if (asset == null)
            return null;

        // Attach raw JSON for debugging / raw viewer
        asset.RawJson = assetContent;

        return asset;
    }

    // Fetches relationships for a given asset id from Pureservice, including linked tickets and users.
    public async Task<PureserviceRelationshipSearchResponse?> GetRelationshipsByAssetIdAsync(string assetId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(assetId))
            throw new ArgumentException("Resurs-id kan ikke være null eller en tom string.", nameof(assetId));

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Send GET request to Pureservice API
        var include = "type,type.relationshipTypeGroup,toAsset,toTicket,toTicket.assignedAgent," +
                      "toChange,toChange.coordinator,toUser,toCompany";

        var filter =  "(toAssetId != NULL && !toAsset.Type.Disabled) || " +
                      "toTicketId != NULL || " +
                      "toChangeId != NULL || " +
                      "toCompanyId != NULL || " +
                      "toUserId != NULL";

        var url = $"relationship/{assetId}/fromAsset" +
                  $"?include={Uri.EscapeDataString(include)}" +
                  $"&filter={Uri.EscapeDataString(filter)}";

        // Send GET request to Pureservice API
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Pureservice | Relationship", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Pureservice | Relationship", url, (int)response.StatusCode))
            return null;

        // Deserialize response
        var result = JsonSerializer.Deserialize<PureserviceRelationshipSearchResponse>( content, _jsonSerializerOptions);

        // Attach raw JSON for debugging / raw viewer
        if (result is not null)
            result.RawJson = content;

        return result;
    }

    #endregion
}

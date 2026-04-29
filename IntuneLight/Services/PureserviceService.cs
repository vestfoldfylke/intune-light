using IntuneLight.Infrastructure;
using IntuneLight.Models.Offboarding;
using IntuneLight.Models.Pureservice;
using IntuneLight.Security;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IntuneLight.Services;

public interface IPureserviceService
{
    Task<PureserviceUser?> GetUserByEmailAsync(string email);
    Task<PureserviceTicket?> GetTicketByRequestNumberAsync(string requestNumber);
    Task<PureserviceAsset?> GetAssetBySerialAsync(string serialNumber);
    Task<PureserviceRelationshipSearchResponse?> GetRelationshipsByAssetIdAsync(string assetId);
    Task<PureserviceTicket?> CreateOffboardingTicketAsync(string subject, string description, int userId, int assetId, OffboardingRoutine routine, string? userUpn);
    Task<string?> GetAssetTypeClassNameAsync(int typeId);
    Task<List<PureserviceAssetStatus>> GetAssetStatusTypesAsync(int assetTypeId);
    Task EnsureConfigCacheAsync();
}

public sealed class PureserviceService
(
    IHttpClientFactory httpClientFactory, 
    ITokenService tokenService, 
    IApiResponseGuard guard,
    IOptions<PureserviceOffboardingOptions> offboardingOptions,
    PureserviceConfigCache pureserviceConfigCache,
    UserContext userContext) : IPureserviceService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;
    private readonly PureserviceConfigCache _configCache = pureserviceConfigCache;
    private readonly PureserviceOffboardingOptions _offboardingOptions = offboardingOptions.Value;
    private readonly UserContext _userContext = userContext;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Looks up a Pureservice user by primary email address.
    public async Task<PureserviceUser?> GetUserByEmailAsync(string email)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            email,
            nameof(email),
            systemName: SystemNames.PureserviceUser,
            userMessage: "E-post kan ikke være tom.");

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceUser, url, content))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.PureserviceUser, url, (int)response.StatusCode))
        {
            return null;
        }

        // Deserialize response and return first user found
        var payload = JsonSerializer.Deserialize<PureserviceUserSearchResponse>(content, _jsonSerializerOptions);
        var pureserviceUser = payload?.Users?.FirstOrDefault();

        // Attach raw json
        if (pureserviceUser != null)
        {
            pureserviceUser.RawJson = content;
        }

        return pureserviceUser;
    }

    // Looks up a Pureservice ticket by ticket id.
    public async Task<PureserviceTicket?> GetTicketByRequestNumberAsync(string requestNumber)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            requestNumber,
            nameof(requestNumber),
            systemName: SystemNames.PureserviceTicket,
            userMessage: "Saksnummer kan ikke være tomt.");


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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceTicket, url, content))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.PureserviceTicket, url, (int)response.StatusCode))
        {
            return null;
        }

        // Deserialize response and return ticket
        var payload = JsonSerializer.Deserialize<PureserviceTicketSearchResponse>(content, _jsonSerializerOptions);
        var pureserviceTicket = payload?.Tickets?.FirstOrDefault();

        // Attach raw json
        if (pureserviceTicket != null)
        {
            pureserviceTicket.RawJson = content;
        }

        return pureserviceTicket;
    }

    // Looks up a Pureservice asset by serial number using the asset endpoint. (minimal response)
    public async Task<PureserviceAsset?> GetAssetBySerialAsync(string serialNumber)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            serialNumber,
            nameof(serialNumber),
            systemName: SystemNames.PureserviceDevice,
            userMessage: "Serienummer kan ikke være tomt.");

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL with filter for serial number
        var filter = Uri.EscapeDataString($"uniqueId == \"{serialNumber}\"");
        var searchUrl = $"asset/?filter={filter}";

        // Search asset by serial number(minimal response)
        var searchResponse = await client.GetAsync(searchUrl);
        var searchContent = await searchResponse.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(searchResponse, SystemNames.PureserviceDevice, searchUrl, searchContent))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(searchContent, SystemNames.PureserviceDevice, searchUrl, (int)searchResponse.StatusCode))
        {
            return null;
        }

        // Deserialize search response
        var searchPayload = JsonSerializer.Deserialize<PureserviceAssetSearchResponse>(
            searchContent, _jsonSerializerOptions);

        // If no assets found, return null
        var searchAsset = searchPayload?.Assets?.FirstOrDefault();
        if (searchAsset == null || string.IsNullOrWhiteSpace(searchAsset.Id.ToString()))
        {
            return null;
        }

        // Fetch full asset details by asset ID
        var encodedAssetId = Uri.EscapeDataString(searchAsset.Id.ToString());
        var assetUrl = $"asset/{encodedAssetId}";

        // Find asset by ID (full response)
        var assetResponse = await client.GetAsync(assetUrl);
        var assetContent = await assetResponse.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(assetResponse, SystemNames.PureserviceDevice, assetUrl, assetContent))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(assetContent, SystemNames.PureserviceDevice, assetUrl, (int)assetResponse.StatusCode))
        {
            return null;
        }

        // Deserialize full asset response
        var assetPayload = JsonSerializer.Deserialize<PureserviceAssetSearchResponse>(
            assetContent, _jsonSerializerOptions);

        // If no assets found, return null
        var asset = assetPayload?.Assets?.FirstOrDefault();
        if (asset == null)
        {
            return null;
        }

        // Attach raw JSON for debugging / raw viewer
        asset.RawJson = assetContent;

        return asset;
    }

    // Fetches relationships for a given asset id from Pureservice, including linked tickets and users.
    public async Task<PureserviceRelationshipSearchResponse?> GetRelationshipsByAssetIdAsync(string assetId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            assetId,
            nameof(assetId),
            systemName: SystemNames.PureserviceRelationship,
            userMessage: "Resurs-id kan ikke være tomt.");

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceRelationship, url, content))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.PureserviceRelationship, url, (int)response.StatusCode))
        {
            return null;
        }

        // Deserialize response
        var result = JsonSerializer.Deserialize<PureserviceRelationshipSearchResponse>( content, _jsonSerializerOptions);

        // Attach raw JSON for debugging / raw viewer
        if (result is not null)
        {
            result.RawJson = content;
        }

        return result;
    }

    // Gets the unique class name for an asset type by type id.
    public async Task<string?> GetAssetTypeClassNameAsync(int typeId)
    {
        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL
        var url = $"assettype/{typeId}";

        // Send GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceDevice, url, content))
        {
            return null;
        }

        // Deserialize and return class name
        var payload = JsonSerializer.Deserialize<JsonElement>(content, _jsonSerializerOptions);
        return payload.GetProperty("assettypes")
                      .EnumerateArray()
                      .FirstOrDefault()
                      .GetProperty("uniqueClassName")
                      .GetString();
    }

    // Fetches all asset status types for a given asset type ID.
    public async Task<List<PureserviceAssetStatus>> GetAssetStatusTypesAsync(int assetTypeId)
    {
        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Build request URL
        var url = $"assettype/{assetTypeId}?include=statuses";

        // Send GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceAssetStatusTypes, url, content))
        {
            return [];
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.PureserviceAssetStatusTypes, url, (int)response.StatusCode))
        {
            return [];
        }

        // Deserialize and return statuses
        var payload = JsonSerializer.Deserialize<PureserviceAssetTypeResponse>(content, _jsonSerializerOptions);
        return payload?.Linked?.AssetStatuses ?? [];
    }

    // Ensures all static PureService configuration data is cached. Fetches from API if not already cached.
    public async Task EnsureConfigCacheAsync()
    {
        if (_configCache.TicketStatuses.Count > 0)
        {
            return;
        }

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Fetch all static config data in parallel
        var responses = await Task.WhenAll(
            client.GetAsync("status/"),
            client.GetAsync("tickettype/"),
            client.GetAsync("priority/"),
            client.GetAsync("source/"),
            client.GetAsync("requesttype/"),
            client.GetAsync("category/"),
            client.GetAsync("relationshiptype/"));

        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));

        // Deserialize and cache each response
        _configCache.TicketStatuses = DeserializeCacheResponse<PureserviceTicketStatusSearchResponse, PureserviceTicketStatus>(
            responses[0], contents[0], "status/", r => r.Statuses);

        _configCache.TicketTypes = DeserializeCacheResponse<PureserviceTicketTypeSearchResponse, PureserviceTicketType>(
            responses[1], contents[1], "tickettype/", r => r.TicketTypes);

        _configCache.Priorities = DeserializeCacheResponse<PureservicePrioritySearchResponse, PureservicePriority>(
            responses[2], contents[2], "priority/", r => r.Priorities);

        _configCache.Sources = DeserializeCacheResponse<PureserviceSourceSearchResponse, PureserviceSource>(
            responses[3], contents[3], "source/", r => r.Sources);

        _configCache.RequestTypes = DeserializeCacheResponse<PureserviceRequestTypeSearchResponse, PureserviceRequestType>(
            responses[4], contents[4], "requesttype/", r => r.RequestTypes);

        _configCache.Categories = DeserializeCacheResponse<PureserviceCategorySearchResponse, PureserviceCategory>(
            responses[5], contents[5], "category/", r => r.Categories);

        _configCache.RelationshipTypes = DeserializeCacheResponse<PureserviceRelationshipTypeSearchResponse, PureserviceRelationshipType>(
            responses[6], contents[6], "relationshiptype/", r => r.RelationshipTypes);
    }

    #endregion

    #region Put requests

    // Creates a new offboarding ticket in PureService, links it to the given asset, and assigns it to the service agent.
    public async Task<PureserviceTicket?> CreateOffboardingTicketAsync(
        string subject,
        string description,
        int userId,
        int assetId,
        OffboardingRoutine routine,
        string? userUpn)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            subject,
            nameof(subject),
            systemName: SystemNames.PureserviceOffboardingTicket,
            userMessage: "Emne kan ikke være tomt.");

        // Ensure config cache is populated
        await EnsureConfigCacheAsync();

        // Validate resolved status
        var resolvedStatus = _configCache.TicketStatuses.FirstOrDefault(s => s.Name == PureserviceNames.TicketStatusResolved);
        UiValidation.RequireNotNull(
            resolvedStatus,
            nameof(resolvedStatus),
            systemName: SystemNames.PureserviceOffboardingTicket,
            userMessage: $"Finner ikke status '{PureserviceNames.TicketStatusResolved}' i PureService. Kontakt administrator.");

        // Resolve ticket field values from config cache
        var ticketTypeId = _configCache.TicketTypes.FirstOrDefault(t => t.Name == PureserviceNames.TicketTypeName)?.Id ?? 0;
        var priorityId = _configCache.Priorities.FirstOrDefault(p => p.Name == PureserviceNames.PriorityName && p.RequestTypeId == 1)?.Id ?? 0;
        var sourceId = _configCache.Sources.FirstOrDefault(s => s.Name == PureserviceNames.SourceName)?.Id ?? 0;
        var category1Id = _configCache.Categories.FirstOrDefault(c => c.Name == PureserviceNames.Category1Name)?.Id ?? 17; // Fallback used in dev — "Klient" (id 17) substitutes for "Maskinvare"
        var category2Id = _configCache.Categories.FirstOrDefault(c => c.Name == PureserviceNames.Category2Name)?.Id ?? 2;  // Fallback used in dev — "PC" exists in both environments
        var category3Id = _configCache.Categories.FirstOrDefault(c => c.Name == PureserviceNames.Category3Name)?.Id ?? 22; // Fallback used in dev — "Privatisering" (id 22)
        var requestTypeId = _configCache.RequestTypes.FirstOrDefault(r => r.Key == PureserviceNames.RequestTypeName)?.Id ?? 0;

        var isStudent = userUpn?.Contains(OrganizationConstants.StudentEmailDomain) == true;

        var relationshipTypeName = routine == OffboardingRoutine.Privatization
            ? (isStudent ? PureserviceNames.RelationshipTypePrivatizationStudent : PureserviceNames.RelationshipTypePrivatizationEmployee)
            : PureserviceNames.RelationshipTypeDeletion;

        var relationshipTypeId = _configCache.RelationshipTypes
            .FirstOrDefault(r => r.Name == relationshipTypeName)?.Id ?? 0;

        // Validate resolved field values
        if (relationshipTypeId == 0)
        {
            throw new UiValidationException(
                systemName: SystemNames.PureserviceOffboardingTicket,
                message: $"Finner ikke relasjonstype '{relationshipTypeName}' i PureService. Kontakt administrator.");
        }

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        var tempId = $"relationship-{Guid.NewGuid()}";

        // Build ticket payload
        var payload = new
        {
            tickets = new[]
            {
                new
                {
                    subject,
                    description,
                    origin = 1,
                    isMarkedForDeletion = false,
                    visibility = 2,
                    solution = "<p>Saken er håndtert automatisk av Intune Light.</p>",
                    links = new
                    {
                        user               = new { id = userId },
                        ticketType         = new { id = ticketTypeId },
                        priority           = new { id = priorityId },
                        status             = new { id = resolvedStatus!.Id },
                        source             = new { id = sourceId },
                        assignedDepartment = new { id = _offboardingOptions.DepartmentId },
                        category1          = new { id = category1Id },
                        category2          = new { id = category2Id },
                        category3          = new { id = category3Id },
                        requestType        = new { id = requestTypeId },
                        relationships      = new[] { new { temporaryId = tempId, type = "relationship" } }
                    }
                }
            },
            linked = new
            {
                relationships = new[]
                {
                    new
                    {
                        toAssetId           = assetId,
                        main                = "ToAssetId",
                        inverseMain         = "FromAssetId",
                        solvingRelationship = false,
                        links = new
                        {
                            type    = new { id = relationshipTypeId },
                            toAsset = new { id = assetId }
                        },
                        temporaryId = tempId
                    }
                }
            }
        };

        // Serialize and send POST request
        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

        var response = await client.PostAsync("ticket/", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success
        _guard.EnsureSuccess(response, SystemNames.PureserviceOffboardingTicket, "ticket/", content);

        // Deserialize created ticket
        var result = JsonSerializer.Deserialize<PureserviceTicketSearchResponse>(content, _jsonSerializerOptions);
        var ticket = result?.Tickets?.FirstOrDefault();

        if (ticket is null)
        {
            return null;
        }

        // Fetch full ticket for PUT
        var getUrl = $"ticket/{ticket.Id}";
        var getResponse = await client.GetAsync(getUrl);
        var getContent = await getResponse.Content.ReadAsStringAsync();

        if (!_guard.EnsureSuccessOrNoData(getResponse, SystemNames.PureserviceOffboardingTicket, getUrl, getContent))
        {
            return ticket;
        }

        // Deserialize and set assignedAgentId directly on flat object
        var ticketObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonDocument.Parse(getContent).RootElement.GetProperty("tickets").EnumerateArray().First().GetRawText())!;


        // Try to lookup agent by upn
        var agentId = _offboardingOptions.AgentId;
        if (!string.IsNullOrWhiteSpace(_userContext.Upn))
        {
            var normalizedAgentEmail = NormalizeEmailForLookup(_userContext.Upn);
            var pureserviceAgent = await GetUserByEmailAsync(normalizedAgentEmail);
            if (pureserviceAgent is not null)
            {
                agentId = pureserviceAgent.Id;
            }
        }

        ticketObj["assignedAgentId"] = JsonSerializer.SerializeToElement(agentId);

        var putPayload = new Dictionary<string, object> { ["tickets"] = new[] { ticketObj } };
        var putJson = JsonSerializer.Serialize(putPayload, _jsonSerializerOptions);
        using var putContent = new StringContent(putJson, Encoding.UTF8, "application/vnd.api+json");

        var putResponse = await client.PutAsync($"ticket/{ticket.Id}", putContent);
        var putResponseContent = await putResponse.Content.ReadAsStringAsync();

        _guard.EnsureSuccess(putResponse, SystemNames.PureserviceOffboardingTicket, $"ticket/{ticket.Id}", putResponseContent);

        return ticket;
    }

    #endregion

    #region Helper methods

    // Normalizes an email address by removing identifiers (.s. or .l.) for PureService lookup.
    private static string NormalizeEmailForLookup(string email)
    {
        return Regex.Replace(email, @"\.[sSlL]\.", ".", RegexOptions.IgnoreCase);
    }

    // Deserializes a cached API response and returns the result, or an empty list if the response is invalid.
    private List<T> DeserializeCacheResponse<TResponse, T>(
        HttpResponseMessage response,
        string content,
        string url,
        Func<TResponse, List<T>?> selector) where TResponse : class
    {
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.PureserviceConfigCache, url, content))
        {
            return [];
        }

        if (!_guard.EnsureJsonBody(content, SystemNames.PureserviceConfigCache, url, (int)response.StatusCode))
        {
            return [];
        }

        var payload = JsonSerializer.Deserialize<TResponse>(content, _jsonSerializerOptions);
        return payload is null ? [] : selector(payload) ?? [];
    }

    #endregion
}
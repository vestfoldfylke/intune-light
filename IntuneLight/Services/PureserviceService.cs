using IntuneLight.Infrastructure;
using IntuneLight.Models.Pureservice;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntuneLight.Services;

public interface IPureserviceService
{
    Task<PureserviceUser?> GetUserByEmailAsync(string email);
    Task<PureserviceTicket?> GetTicketByRequestNumberAsync(string requestNumber);
    Task<PureserviceAsset?> GetAssetBySerialAsync(string serialNumber);
    Task<PureserviceRelationshipSearchResponse?> GetRelationshipsByAssetIdAsync(string assetId);
    Task UpdateAssetStatusAsync(string assetId, int typeId);
    Task<PureserviceTicket?> CreateOffboardingTicketAsync(string subject, string description, int userId, int assetId);
    Task<string?> GetAssetTypeClassNameAsync(int typeId);
}

public sealed class PureserviceService
(
    IHttpClientFactory httpClientFactory, 
    ITokenService tokenService, 
    IApiResponseGuard guard,
    IOptions<PureserviceOffboardingOptions> offboardingOptions) : IPureserviceService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;
    private readonly PureserviceOffboardingOptions _offboardingOptions = offboardingOptions.Value;

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
        const int limit = 5;
        //var encodedQuery = Uri.EscapeDataString(serialNumber);
        var encodedQuery = Uri.EscapeDataString($"\"{serialNumber}\"");
        var searchUrl = $"asset/search?query={encodedQuery}&limit={limit}";

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

    #endregion

    #region Put requests
    // Updates the status of a Pureservice asset to "Sold" by asset id.
    public async Task UpdateAssetStatusAsync(string assetId, int typeId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            assetId,
            nameof(assetId),
            systemName: SystemNames.PureserviceAssetStatus,
            userMessage: "Resurs-id kan ikke være tomt.");

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        // Fetch current asset to get full payload for update
        var getUrl = $"asset/{Uri.EscapeDataString(assetId)}";
        var getResponse = await client.GetAsync(getUrl);
        var getContent = await getResponse.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(getResponse, SystemNames.PureserviceAssetStatus, getUrl, getContent))
        {
            throw new InvalidOperationException("Kunne ikke hente asset for oppdatering.");
        }

        // Fetch asset type class name required as root key in PUT payload
        var className = await GetAssetTypeClassNameAsync(typeId);
        if (string.IsNullOrWhiteSpace(className))
        {
            throw new InvalidOperationException($"Kunne ikke hente asset type klassenavn for typeId {typeId}.");
        }

        // Parse asset and update only the status link
        var doc = JsonDocument.Parse(getContent);
        var assetElement = doc.RootElement.GetProperty("assets").EnumerateArray().First();
        var assetDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(assetElement.GetRawText())!;
        var linksDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(assetDict["links"].GetRawText())!;

        // Replace status with sold status id from options
        linksDict["status"] = JsonSerializer.SerializeToElement(new { id = _offboardingOptions.AssetStatusSoldId });
        assetDict["links"] = JsonSerializer.SerializeToElement(linksDict);

        // Build payload with asset type class name as root key
        var payload = new Dictionary<string, object>
        {
            [className] = new[] { assetDict }
        };

        // Serialize payload to JSON
        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

        // Send the PUT request
        var putUrl = $"asset/{Uri.EscapeDataString(assetId)}";
        var response = await client.PutAsync(putUrl, httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success
        _guard.EnsureSuccess(response, SystemNames.PureserviceAssetStatus, putUrl, content);
    }

    // Creates a new offboarding ticket in Pureservice, links it to the given asset, and assigns it to the given agent.
    public async Task<PureserviceTicket?> CreateOffboardingTicketAsync(string subject, string description, int userId, int assetId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            subject,
            nameof(subject),
            systemName: SystemNames.PureserviceOffboardingTicket,
            userMessage: "Emne kan ikke være tomt.");

        // Create named HTTP client
        var client = _httpClientFactory.CreateClient("Pureservice");

        // Set headers and fetch api key
        var apiKey = await _tokenService.GetPureserviceTokenAsync();
        client.DefaultRequestHeaders.Remove("X-Authorization-Key");
        client.DefaultRequestHeaders.Add("X-Authorization-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

        var temporaryId = $"relationship-{Guid.NewGuid()}";

        // Build request URL
        var postUrl = "ticket/";

        // Build ticket payload based on Pureservice API documentation
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
                    visibility = 2, // 0 = Visible, 2 = Not Visible
                    links = new
                    {
                        user = new { id = userId },
                        ticketType = new { id = _offboardingOptions.TicketTypeId },
                        priority = new { id = _offboardingOptions.PriorityId },
                        status = new { id = _offboardingOptions.StatusId },
                        source = new { id = _offboardingOptions.SourceId },
                        assignedAgent = new { id = _offboardingOptions.AgentId },
                        assignedTeam = new { id = _offboardingOptions.TeamId },
                        assignedDepartment = new { id = _offboardingOptions.DepartmentId },
                        category1 = new { id = _offboardingOptions.Category1Id },
                        category2 = new { id = _offboardingOptions.Category2Id },
                        requestType = new { id = _offboardingOptions.RequestTypeId },
                        relationships = new[] { new { temporaryId, type = "relationship" } }
                    }
                }
            },
            linked = new
            {
                relationships = new[]
                {
                    new
                    {
                        toAssetId = assetId,
                        main = "ToAssetId",
                        inverseMain = "FromAssetId",
                        solvingRelationship = false,
                        links = new
                        {
                            type = new { id = _offboardingOptions.RelationshipTypeId },
                            toAsset = new { id = assetId }
                        },
                        temporaryId
                    }
                }
            }
        };

        // Serialize payload to JSON
        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

        // Send the POST request to create the ticket
        var response = await client.PostAsync(postUrl, httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success
        _guard.EnsureSuccess(response, SystemNames.PureserviceOffboardingTicket, postUrl, content);

        // Deserialize created ticket
        var result = JsonSerializer.Deserialize<PureserviceTicketSearchResponse>(content, _jsonSerializerOptions);
        var ticket = result?.Tickets?.FirstOrDefault();

        if (ticket is null)
        {
            return null;
        }

        // Fetch the full ticket to get all fields needed for PUT
        var getUrl = $"ticket/{ticket.Id}";
        var getResponse = await client.GetAsync(getUrl);
        var getContent = await getResponse.Content.ReadAsStringAsync();

        if (!_guard.EnsureSuccessOrNoData(getResponse, SystemNames.PureserviceOffboardingTicket, getUrl, getContent))
        {
            return ticket;
        }

        // Parse ticket and update assignedAgent
        var doc = JsonDocument.Parse(getContent);
        var ticketElement = doc.RootElement.GetProperty("tickets").EnumerateArray().First();
        var ticketDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ticketElement.GetRawText())!;
        var linksDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ticketDict["links"].GetRawText())!;

        // Set assigned agent
        linksDict["assignedAgent"] = JsonSerializer.SerializeToElement(new { id = _offboardingOptions.AgentId.ToString(), type = "user" });
        ticketDict["links"] = JsonSerializer.SerializeToElement(linksDict);

        // Build PUT payload
        var putPayload = new Dictionary<string, object>
        {
            ["tickets"] = new[] { ticketDict }
        };

        // Serialize PUT payload
        var putJson = JsonSerializer.Serialize(putPayload, _jsonSerializerOptions);
        using var putContent = new StringContent(putJson, Encoding.UTF8, "application/vnd.api+json");

        // Send the PUT request to assign the agent
        var putUrl = $"ticket/{ticket.Id}";
        var putResponse = await client.PutAsync(putUrl, putContent);
        var putResponseContent = await putResponse.Content.ReadAsStringAsync();

        // Ensure success
        _guard.EnsureSuccess(putResponse, SystemNames.PureserviceOffboardingTicket, putUrl, putResponseContent);

        return ticket;
    }

    #endregion
}
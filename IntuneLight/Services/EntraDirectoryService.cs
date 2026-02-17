using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Entra;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace IntuneLight.Services;

public interface IEntraDirectoryService
{
    Task DeleteDeviceByAzureAdDeviceIdAsync(string azureAdDeviceId);
    Task<EntraDevice?> GetDeviceByAzureAdDeviceIdAsync(string azureAdDeviceId);
    Task<EntraDeviceCount?> GetRegisteredDevicesForUser(string userId);
    Task<EntraUser?> GetUserByUpnAsync(string upn);
    Task<byte[]?> GetUserPhotoAsync(string userIdOrUpn);
}

public sealed class EntraDirectoryService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IApiResponseGuard guard) : IEntraDirectoryService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Fetch user by UPN from Entra ID (Azure AD)
    public async Task<EntraUser?> GetUserByUpnAsync(string upn)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            upn,
            nameof(upn),
            systemName: SystemNames.EntraUser,
            userMessage: "UPN kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL (filter: ?$select=id,displayName,mail,userPrincipalName)
        var filter = "?$select=id,displayName,jobTitle,mobilePhone,officeLocation,userPrincipalName,accountEnabled,department";
        var url = $"v1.0/users/{Uri.EscapeDataString(upn)}" + filter;

        // Send the GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.EntraUser, url, content))
            return null;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.EntraUser, url, (int)response.StatusCode))
            return null;

        // Deserialize the response content to EntraUser
        EntraUser? entraUser = JsonSerializer.Deserialize<EntraUser>(content, _jsonSerializerOptions);

        // Attach raw JSON
        if (entraUser != null)
            entraUser.RawJson = content;

        // Fetch manager for employees
        if (entraUser != null && !entraUser.UserPrincipalName.Contains("skole"))
        {
            // Build the request URL for manager
            url = $"v1.0/users/{Uri.EscapeDataString(upn)}/manager";

            // Send the GET request to get manager
            response = await client.GetAsync(url);
            content = await response.Content.ReadAsStringAsync();

            // Ensure success or treat no-data as valid
            if (!_guard.EnsureSuccessOrNoData(response, SystemNames.EntraManager, url, content))
                return entraUser;

            // Ensure JSON body
            if (!_guard.EnsureJsonBody(content, SystemNames.EntraManager, url, (int)response.StatusCode))
                return entraUser;

            // Deserialize the response content to EntraUser
            var manager = JsonSerializer.Deserialize<EntraUser>(content, _jsonSerializerOptions);

            // Insert manager into entra user obj
            if (manager is not null)
                entraUser.Manager = manager.DisplayName;
        }

        return entraUser;
    }

    /// Resolves an Entra device from Microsoft Graph using the Azure AD device id
    public async Task<EntraDevice?> GetDeviceByAzureAdDeviceIdAsync(string azureAdDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            azureAdDeviceId,
            nameof(azureAdDeviceId),
            systemName: SystemNames.EntraDevice,
            userMessage: "Enhets-ID (Azure AD) kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/devices(deviceId='{Uri.EscapeDataString(azureAdDeviceId)}')"; // 

        // Send the GET request.
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.EntraDevice, url, content))
            return null;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.EntraDevice, url, (int)response.StatusCode))
            return null;

        // Deserialize the response content to EntraDevice.
        var entraDevice = JsonSerializer.Deserialize<EntraDevice>(content, _jsonSerializerOptions);

        // Attach raw JSON
        if (entraDevice is not null)
            entraDevice.RawJson = content;

        return entraDevice;
    }

    // Fetches the profile photo for an Entra user by id or UPN.
    public async Task<byte[]?> GetUserPhotoAsync(string userIdOrUpn)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            userIdOrUpn,
            nameof(userIdOrUpn),
            systemName: SystemNames.EntraUserPhoto,
            userMessage: "Bruker-ID eller UPN kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/users/{Uri.EscapeDataString(userIdOrUpn)}/photos/120x120/$value";

        // Send the GET request.
        var response = await client.GetAsync(url);

        // 404 = user has no photo → not an error
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        // Read response
        var content = await response.Content.ReadAsByteArrayAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, SystemNames.EntraUserPhoto, url, $"<binary {content.Length} bytes>");

        // Ensure binary body
        if (!_guard.EnsureBinaryBody(content, SystemNames.EntraUserPhoto, url, (int)response.StatusCode))
            return null;
            
        return content;
    }

    /// Resolves an Entra device from Microsoft Graph using the Azure AD device id
    public async Task<EntraDeviceCount?> GetRegisteredDevicesForUser(string userId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            userId,
            nameof(userId),
            systemName: SystemNames.EntraUserDevices,
            userMessage: "Bruker-ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/users/{Uri.EscapeDataString(userId)}/registeredDevices?$select=displayName";

        // Send the GET request.
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.EntraUserDevices, url, content))
            return null;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.EntraUserDevices, url, (int)response.StatusCode))
            return null;

        // Deserialize the response content to EntraDevice.
        var entraDeviceCount = JsonSerializer.Deserialize<EntraDeviceCount>(content, _jsonSerializerOptions);

        // Attach raw JSON
        if (entraDeviceCount != null)
            entraDeviceCount.RawJson = content;

        return entraDeviceCount;
    }


    #endregion

    #region Delete requests

    // Permanently deletes an Entra device from Microsoft Graph using the Azure AD device id.
    public async Task DeleteDeviceByAzureAdDeviceIdAsync(string azureAdDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            azureAdDeviceId,
            nameof(azureAdDeviceId),
            systemName: SystemNames.EntraDeviceDelete,
            userMessage: "Enhets-ID (Azure AD) kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/devices(deviceId='{Uri.EscapeDataString(azureAdDeviceId)}')";

        // Send the DELETE request
        var response = await client.DeleteAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, SystemNames.EntraDeviceDelete, url, content);
    }


    #endregion
}

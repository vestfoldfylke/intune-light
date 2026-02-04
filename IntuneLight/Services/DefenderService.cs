using System.Net.Http.Headers;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Defender;

namespace IntuneLight.Services;

public interface IDefenderService
{
    Task<DefenderDevice?> GetDeviceByAadDeviceIdAsync(string aadDeviceId);
    Task<DefenderDevice?> GetDeviceByHostnameAsync(string hostname);
    Task<bool> GetIsolationStatusByMachineId(string machineId);
}

public sealed class DefenderService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IApiResponseGuard guard) : IDefenderService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Fetch device by AAD device id from Microsoft Defender
    public async Task<DefenderDevice?> GetDeviceByAadDeviceIdAsync(string aadDeviceId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(aadDeviceId))
            throw new ArgumentException("AadDeviceId kan ikke være null eller en tom string.", nameof(aadDeviceId));

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Defender");
        var token = await _tokenService.GetDefenderTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL with the filter for aadDeviceId
        var filter = $"aadDeviceId eq {aadDeviceId}";
        var url = $"api/machines?$filter={Uri.EscapeDataString(filter)}&$top=1";

        // Create named HTTP client and fetch token
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Defender | Device", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Defender | Device", url, (int)response.StatusCode))
            return null;

        // Deserialize the response content
        DefenderDeviceListResponse? payload = JsonSerializer.Deserialize<DefenderDeviceListResponse>(content, _jsonSerializerOptions);

        // Find the first device and attach raw JSON
        var defenderDevice = payload?.Value?.FirstOrDefault();
        if (defenderDevice != null)
            defenderDevice.RawJson = content;

        return defenderDevice;
    }

    // Fetch device by hostname from Microsoft Defender
    public async Task<DefenderDevice?> GetDeviceByHostnameAsync(string hostname)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(hostname))
            throw new ArgumentException("Hostname kan ikke være null eller en tom string.", nameof(hostname));

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Defender");
        var token = await _tokenService.GetDefenderTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL with the filter for hostname
        var escaped = hostname.Replace("'", "''");
        var url = $"api/machines" + 
                  $"?$filter=computerDnsName eq '{escaped}'" + 
                  "&$top=1";

        // Send the GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Defender | Device", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Defender | Device", url, (int)response.StatusCode))
            return null;

        // Deserialize the response content
        DefenderDeviceListResponse? payload = JsonSerializer.Deserialize<DefenderDeviceListResponse>(content, _jsonSerializerOptions);

        // Find the first device and attach raw JSON
        var defenderDevice = payload?.Value?.FirstOrDefault();
        if (defenderDevice != null)
            defenderDevice.RawJson = content;

        return defenderDevice;
    }

    // Fetch device isolation-status by machine-id
    public async Task<bool> GetIsolationStatusByMachineId(string machineId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(machineId))
            throw new ArgumentException("Machine ID kan ikke være null eller en tom string.", nameof(machineId));       

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Defender");
        var token = await _tokenService.GetDefenderTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL with the filter for machine-id
        var escaped = machineId.Replace("'", "''");
        //var escaped = "4743f014-1ef6-4fd2-ae1b-fc78fefe1b72";
        var url = $"api/machineactions?$filter=type eq 'Isolate' and machineId eq '{escaped}'";

        // Send the GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Defender | Device", url, content);

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, "Defender | Isolate", url, (int)response.StatusCode))
            return false;

        // Deserialize the response content
        DefenderMachineActions? payload = JsonSerializer.Deserialize<DefenderMachineActions>(content, _jsonSerializerOptions);

        // Determine if the device is isolated
        if (payload is not null)
        {
            return payload.Value.Any(a =>
                a.Type == "Isolate" &&
                a.Status == "Succeeded");
        }

        return false;
    }

    #endregion
}
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Defender;
using Vestfold.Extensions.Metrics.Services;

namespace IntuneLight.Services;

public interface IDefenderService
{
    Task<DefenderDevice?> GetDeviceByAadDeviceIdAsync(string aadDeviceId);
    Task<DefenderDevice?> GetDeviceByHostnameAsync(string hostname);
    Task<bool> GetIsolationStatusByMachineId(string machineId);
    Task<DefenderScanResult> RunAntiVirusScanAsync(string machineId, DefenderScanType scanType, string? comment = null);
}

public sealed class DefenderService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IApiResponseGuard guard, IMetricsService metricsService) : IDefenderService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;
    private readonly IMetricsService _metricsService = metricsService;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Fetch device by AAD device id from Microsoft Defender
    public async Task<DefenderDevice?> GetDeviceByAadDeviceIdAsync(string aadDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            aadDeviceId,
            nameof(aadDeviceId),
            systemName: SystemNames.DefenderDevice,
            userMessage: "Enhets-ID (AAD) kan ikke være tom.");

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

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.DefenderDevice, url, content))
            return null;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.DefenderDevice, url, (int)response.StatusCode))
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
        UiValidation.RequireNotNullOrWhiteSpace(
            hostname,
            nameof(hostname),
            systemName: SystemNames.DefenderDevice,
            userMessage: "Maskinnavn kan ikke være tomt.");

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

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.DefenderDevice, url, content))
            return null;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.DefenderDevice, url, (int)response.StatusCode))
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
        UiValidation.RequireNotNullOrWhiteSpace(
            machineId,
            nameof(machineId),
            systemName: SystemNames.DefenderIsolation,
            userMessage: "Maskin-ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Defender");
        var token = await _tokenService.GetDefenderTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL with the filter for machine-id
        var escaped = machineId.Replace("'", "''");
        var url = $"api/machineactions?$filter=type eq 'Isolate' and machineId eq '{escaped}'";

        // Send the GET request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.DefenderIsolation, url, content))
            return false;

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.DefenderIsolation, url, (int)response.StatusCode))
            return false;

        // Deserialize the response content
        DefenderMachineActions? payload = JsonSerializer.Deserialize<DefenderMachineActions>(content, _jsonSerializerOptions);

        // Determine if the device is isolated
        return payload?.Value?.Any(a => a.Type == "Isolate" && a.Status == "Succeeded") == true;
    }

    #endregion

    #region Post requests

    // Runs a Defender for Endpoint antivirus scan (quick or full) on a machine.
    public async Task<DefenderScanResult> RunAntiVirusScanAsync(string machineId, DefenderScanType scanType, string? comment = null)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            machineId,
            nameof(machineId),
            systemName: SystemNames.DefenderAvScan,
            userMessage: "Machine ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Defender");
        var token = await _tokenService.GetDefenderTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"api/machines/{Uri.EscapeDataString(machineId)}/runAntiVirusScan";

        // Build request body
        var payload = new
        {
            Comment = string.IsNullOrWhiteSpace(comment) ? "Triggered from Intune Light." : comment,
            ScanType = scanType.ToString()
        };

        // Serialize payload to JSON
        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request
        var response = await client.PostAsync(url, httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Handle "already in progress" explicitly (not an error)
        if (response.StatusCode == HttpStatusCode.BadRequest && content.Contains("already in progress", StringComparison.OrdinalIgnoreCase))
            return DefenderScanResult.AlreadyRunning;

        // Ensure success (If successful, this method returns 201, Created response code and MachineAction object in the response body.)
        _guard.EnsureSuccess(response, SystemNames.DefenderAvScan, url, content);

        // If we reach here, the scan was started successfully
        return DefenderScanResult.Started;
    }

    #endregion
}
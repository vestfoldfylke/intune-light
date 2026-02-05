using System.Net.Http.Headers;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Intune;

namespace IntuneLight.Services;

public interface IIntuneService
{
    Task<AutopilotDevice?> GetAutopilotDeviceBySerialAsync(string serialNumber);
    Task<BitlockerRecoveryKey?> GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(string azureAdDeviceId);
    Task<ManagedDevice?> GetDeviceBySerialAsync(string serialNumber);
    Task<DeviceCredential?> GetLapsPasswordByAzureDeviceId(string azureAdDeviceId);
}

public sealed class IntuneService(IHttpClientFactory httpClientFactory, ITokenService tokenService, IApiResponseGuard guard) : IIntuneService
{
    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IApiResponseGuard _guard = guard;

    // JSON serializer options
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #region Get requests

    // Fetch managed device by serial number from Intune
    public async Task<ManagedDevice?> GetDeviceBySerialAsync(string serialNumber)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serienummer kan ikke være null eller en tom string.", nameof(serialNumber));

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build request URL with OData filter for serial number
        var escapedSerial = serialNumber.Replace("'", "''");
        var url = $"beta/deviceManagement/managedDevices" +
                  $"?$filter=serialNumber eq '{escapedSerial}'" +
                  "&$top=1"; 
        
        // Send GET request to Microsoft Graph
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Intune | Device", url, content);

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, "Intune | Device", url, (int)response.StatusCode))
            return null ;

        // Deserialize response to GraphManagedDeviceListResponse
        var payload = JsonSerializer.Deserialize<GraphManagedDeviceListResponse>(content, _jsonSerializerOptions);

        // Find the first device and attach raw JSON
        var intuneDevice = payload?.Value?.FirstOrDefault();
        if (intuneDevice != null)
            intuneDevice.RawJson = content;
        
        return intuneDevice;
    }

    // Fetch LAPS password by Azure AD Device ID
    public async Task<DeviceCredential?> GetLapsPasswordByAzureDeviceId(string azureAdDeviceId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(azureAdDeviceId))
            throw new ArgumentException("AzureADDeviceId kan ikke være null eller en tom string.", nameof(azureAdDeviceId));

        // Validate GUID format
        if (!Guid.TryParse(azureAdDeviceId, out var guid))
            throw new ArgumentException("AzureADDeviceId må være av typen GUID.", nameof(azureAdDeviceId));

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build request URL to get device local credentials
        // Formats the GUID using the standard "D" format (32 hex digits separated by hyphens), e.g., 3f2504e0-4f89-11d3-9a0c-0305e82c3301
        var requestUrl = $"beta/directory/deviceLocalCredentials/{guid:D}?$select=credentials";

        // Create HTTP request message with custom headers
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        requestMessage.Headers.Add("User-Agent", "Dsreg/10.0 (Windows 10.0.26100)");
        requestMessage.Headers.Add("ocp-client-name", "Intune-light-test");
        requestMessage.Headers.Add("ocp-client-version", "1.0");

        // Send the request
        var response = await client.SendAsync(requestMessage);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, "Intune | LAPS", requestUrl, content);

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, "Intune | LAPS", requestUrl, (int)response.StatusCode))
            return null;

        // Deserialize the response content to DeviceCredential
        DeviceCredential? deviceCredentialResponse = JsonSerializer.Deserialize<DeviceCredential>(content, _jsonSerializerOptions);

        // If no credentials found, return null
        if (deviceCredentialResponse == null)
            return null;

        // Decode all passwords from base64
        deviceCredentialResponse.DecodeAllPasswords();

        // Get the latest credential based on BackupDateTime
        var latestCredential = deviceCredentialResponse.Credentials?
            .OrderByDescending(c => c.BackupDateTime)
            .FirstOrDefault();

        // Return a new DeviceCredential with only the latest credential
        return latestCredential != null ?
        new DeviceCredential
        {
            Id = deviceCredentialResponse.Id,
            DeviceName = deviceCredentialResponse.DeviceName,
            LastBackupDateTime = latestCredential.BackupDateTime,
            RefreshDateTime = deviceCredentialResponse.RefreshDateTime,
            Credentials = [latestCredential],
            RawJson = content
        } : null;
    }

    // Fetch Autopilot device by serial number from Intune
    public async Task<AutopilotDevice?> GetAutopilotDeviceBySerialAsync(string serialNumber)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serienummer kan ikke være null eller en tom string.", nameof(serialNumber));
        
        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build request URL with OData filter for serial number
        var filter = $"contains(serialNumber,'{serialNumber}')";
        var url = $"beta/deviceManagement/windowsAutopilotDeviceIdentities?$filter={Uri.EscapeDataString(filter)}&$top=1";

        // Send the request
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Check for unsuccessful response
        _guard.EnsureSuccess(response, "Intune | Autopilot", url, content);

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, "Intune | Autopilot", url, (int)response.StatusCode))
            return null;

        // Deserialize response to AutopilotResponse
        var payload = JsonSerializer.Deserialize<AutopilotResponse>(content, _jsonSerializerOptions);

        // Find the first device and attach raw JSON
        var autopilotDevice = payload?.Value?.FirstOrDefault();
        if (autopilotDevice != null)
            autopilotDevice.RawJson = content;

        return autopilotDevice;
    }

    // Fetch Bitlocker recovery key by Azure AD Device ID
    public async Task<BitlockerRecoveryKey?> GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(string azureAdDeviceId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(azureAdDeviceId))
            throw new ArgumentException("AzureAdDeviceId kan ikke være null eller en tom string.", nameof(azureAdDeviceId));

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build request URL to list Bitlocker recovery keys for the device
        var escaped = azureAdDeviceId.Replace("'", "''");
        var listUrl = $"v1.0/informationProtection/bitlocker/recoveryKeys?$filter=deviceId eq '{escaped}'";

        // Create HTTP request message with custom headers
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, listUrl);
        requestMessage.Headers.Add("User-Agent", "Dsreg/10.0 (Windows 10.0.26100)");
        requestMessage.Headers.Add("ocp-client-name", "Intune-light-test");
        requestMessage.Headers.Add("ocp-client-version", "1.0");

        // Send the request to list recovery keys
        var listResponse = await client.SendAsync(requestMessage);
        var listContent = await listResponse.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(listResponse, "Intune | Bitlocker", listUrl, listContent);

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(listContent, "Intune | Bitlocker", listUrl, (int)listResponse.StatusCode))
            return null;

        // Deserialize the list response to find the most recent recovery key-ID
        var listPayload = JsonSerializer.Deserialize<BitlockerRecoveryKeysResponse>(listContent, _jsonSerializerOptions);
        var candidate = listPayload?.Value?.OrderByDescending(x => x.CreatedDateTime).FirstOrDefault();
        if (candidate == null)
            return null;

        // Build request URL to get the specific recovery key
        var keyUrl = $"v1.0/informationProtection/bitlocker/recoveryKeys/{candidate.Id}?$select=key";

        // Create HTTP request message with custom headers
        requestMessage = new HttpRequestMessage(HttpMethod.Get, keyUrl);
        requestMessage.Headers.Add("User-Agent", "Dsreg/10.0 (Windows 10.0.26100)");
        requestMessage.Headers.Add("ocp-client-name", "Intune-light-test");
        requestMessage.Headers.Add("ocp-client-version", "1.0");

        // Send the request to get the specific recovery key
        var keyResponse = await client.SendAsync(requestMessage);
        var keyContent = await keyResponse.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(keyResponse, "Intune | Bitlocker", keyUrl, keyContent);

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(keyContent, "Intune | Bitlocker", keyUrl, (int)keyResponse.StatusCode))
            return null;

        var keyObj = JsonSerializer.Deserialize<BitlockerRecoveryKey>(keyContent, _jsonSerializerOptions);
        if (keyObj != null)
            keyObj.RawJson = keyContent;

        return keyObj;
    }

    #endregion


}
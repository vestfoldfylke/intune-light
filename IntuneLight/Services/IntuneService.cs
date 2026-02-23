using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Intune;

namespace IntuneLight.Services;

public interface IIntuneService
{
    Task DeleteAutopilotDeviceAsync(string autopilotDeviceId);
    Task DeleteManagedDeviceAsync(string managedDeviceId);
    Task<AutopilotDevice?> GetAutopilotDeviceBySerialAsync(string serialNumber);
    Task<BitlockerRecoveryKey?> GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(string azureAdDeviceId);
    Task<ManagedDevice?> GetDeviceBySerialAsync(string serialNumber);
    Task<DeviceCredential?> GetLapsPasswordByAzureDeviceId(string azureAdDeviceId);
    Task RequestRemoteAssistanceAsync(string managedDeviceId);
    Task RotateLocalAdminPasswordAsync(string managedDeviceId);
    Task SyncManagedDeviceAsync(string managedDeviceId);
    Task UpdateAutopilotGroupTagAsync(string autopilotDeviceId, string groupTag);
    Task WipeManagedDeviceAsync(string managedDeviceId);
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
        UiValidation.RequireNotNullOrWhiteSpace(
            serialNumber,
            nameof(serialNumber),
            systemName: SystemNames.IntuneDevice,
            userMessage: "Serienummer kan ikke være tomt.");

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.IntuneDevice, url, content))
            return null;

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, SystemNames.IntuneDevice, url, (int)response.StatusCode))
            return null;

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
        UiValidation.RequireNotNullOrWhiteSpace(
            azureAdDeviceId,
            nameof(azureAdDeviceId),
            systemName: SystemNames.IntuneLaps,
            userMessage: "Enhets-ID (Azure AD) kan ikke være tom.");

        // Validate GUID format (keep parsed guid for later use)
        if (!Guid.TryParse(azureAdDeviceId, out var guid))
            throw new UiValidationException(
                systemName: SystemNames.IntuneLaps,
                message: "Enhets-ID (Azure AD) har ugyldig format. Forventet GUID.",
                innerException: new ArgumentException(
                    "AzureADDeviceId må være av typen GUID.",
                    nameof(azureAdDeviceId)));

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.IntuneLaps, requestUrl, content))
            return null;

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, SystemNames.IntuneLaps, requestUrl, (int)response.StatusCode))
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
        UiValidation.RequireNotNullOrWhiteSpace(
            serialNumber,
            nameof(serialNumber),
            systemName: SystemNames.IntuneAutopilot,
            userMessage: "Serienummer kan ikke være tomt.");

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.IntuneAutopilot, url, content))
            return null;

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(content, SystemNames.IntuneAutopilot, url, (int)response.StatusCode))
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
        UiValidation.RequireNotNullOrWhiteSpace(
            azureAdDeviceId,
            nameof(azureAdDeviceId),
            systemName: SystemNames.IntuneBitlocker,
            userMessage: "Enhets-ID (Azure AD) kan ikke være tom.");

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

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(listResponse, SystemNames.IntuneBitlocker, listUrl, listContent))
            return null;

        // Ensure body is JSON
        if (!_guard.EnsureJsonBody(listContent, SystemNames.IntuneBitlocker, listUrl, (int)listResponse.StatusCode))
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

    #region Post requests

    // Triggers an Intune sync for a managed device
    public async Task SyncManagedDeviceAsync(string managedDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            managedDeviceId,
            nameof(managedDeviceId),
            systemName: SystemNames.IntuneDeviceSync,
            userMessage: "Intune ManagedDevice ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL (Intune action)
        var url = $"v1.0/deviceManagement/managedDevices/{Uri.EscapeDataString(managedDeviceId)}/syncDevice";

        // Send the POST request (no body required)
        var response = await client.PostAsync(url, content: null);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success (this is an action; must succeed, returns 204 no content on success)
        _guard.EnsureSuccess(response, SystemNames.IntuneDeviceSync, url, content);
    }

    // Wipes an Intune managed device. High impact action (must succeed).
    public async Task WipeManagedDeviceAsync(string managedDeviceId)
    {
        // validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            managedDeviceId,
            nameof(managedDeviceId),
            systemName: SystemNames.IntuneDeviceWipe,
            userMessage: "Intune ManagedDevice ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL (Intune action)
        var url = $"v1.0/deviceManagement/managedDevices/{Uri.EscapeDataString(managedDeviceId)}/wipe";

        // Send minimal request (request body can contain multiple optional parameters)
        using var httpContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success (this is a high-impact action; must succeed, returns 204 no content on success)
        _guard.EnsureSuccess(response, SystemNames.IntuneDeviceWipe, url, content);
    }

    // Requests remote assistance for an Intune managed device.
    public async Task RequestRemoteAssistanceAsync(string managedDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            managedDeviceId,
            nameof(managedDeviceId),
            systemName: SystemNames.IntuneRemoteAssistance,
            userMessage: "Intune ManagedDevice ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/deviceManagement/managedDevices/{Uri.EscapeDataString(managedDeviceId)}/requestRemoteAssistance";

        // Send POST request (no body)
        var response = await client.PostAsync(url, content: null);
        var content = await response.Content.ReadAsStringAsync();

        // Must succeed
        _guard.EnsureSuccess(response, SystemNames.IntuneRemoteAssistance, url, content);
    }

    // Updates Autopilot device properties (groupTag) for a Windows Autopilot device identity.
    public async Task UpdateAutopilotGroupTagAsync(string autopilotDeviceId, string groupTag)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            autopilotDeviceId,
            nameof(autopilotDeviceId),
            systemName: SystemNames.IntuneAutopilotTag,
            userMessage: "Autopilot-enhets-ID kan ikke være tom.");

        UiValidation.RequireNotNullOrWhiteSpace(
            groupTag,
            nameof(groupTag),
            systemName: SystemNames.IntuneAutopilotTag,
            userMessage: "Tag kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/deviceManagement/windowsAutopilotDeviceIdentities/{Uri.EscapeDataString(autopilotDeviceId)}/updateDeviceProperties";

        // Build request body (inferred member)
        var payload = new { groupTag };

        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request (action must succeed)
        var response = await client.PostAsync(url, httpContent);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, SystemNames.IntuneAutopilotTag, url, content);
    }

    // Triggers an Intune action to rotate the local admin password (5–30 mins before pwd changes).
    public async Task RotateLocalAdminPasswordAsync(string managedDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            managedDeviceId,
            nameof(managedDeviceId),
            systemName: SystemNames.IntuneLapsRotate,
            userMessage: "ManagedDevice ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL (beta per Graph documentation)
        var url = $"beta/deviceManagement/managedDevices/{Uri.EscapeDataString(managedDeviceId)}/rotateLocalAdminPassword";

        // Send the POST request (no body required)
        var response = await client.PostAsync(url, content: null);
        var content = await response.Content.ReadAsStringAsync();

        // Action must succeed
        _guard.EnsureSuccess(response, SystemNames.IntuneLapsRotate, url, content);
    }

    #endregion

    #region Delete requests

    // Deletes an Intune managed device record. High impact action (must succeed).
    public async Task DeleteManagedDeviceAsync(string managedDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            managedDeviceId,
            nameof(managedDeviceId),
            systemName: SystemNames.IntuneDeviceDelete,
            userMessage: "Intune ManagedDevice ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/deviceManagement/managedDevices/{Uri.EscapeDataString(managedDeviceId)}";

        // Send the DELETE request
        var response = await client.DeleteAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success (this is a high-impact action; must succeed, returns 204 no content on success)
        _guard.EnsureSuccess(response, SystemNames.IntuneDeviceDelete, url, content);
    }

    // Deletes a Windows Autopilot device identity entry (high impact action; must succeed).
    public async Task DeleteAutopilotDeviceAsync(string autopilotDeviceId)
    {
        // Validate input
        UiValidation.RequireNotNullOrWhiteSpace(
            autopilotDeviceId,
            nameof(autopilotDeviceId),
            systemName: SystemNames.IntuneAutopilotDelete,
            userMessage: "Autopilot-enhets-ID kan ikke være tom.");

        // Create named HTTP client and fetch token
        var client = _httpClientFactory.CreateClient("Graph");
        var token = await _tokenService.GetGraphTokenAsync();

        // Set the Authorization header with the Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Build the request URL
        var url = $"v1.0/deviceManagement/windowsAutopilotDeviceIdentities/{Uri.EscapeDataString(autopilotDeviceId)}";

        // Send the DELETE request
        var response = await client.DeleteAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success using ApiResponseGuard
        _guard.EnsureSuccess(response, SystemNames.IntuneAutopilotDelete, url, content);
    }

    #endregion
}
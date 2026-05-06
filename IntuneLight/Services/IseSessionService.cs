using IntuneLight.Infrastructure;
using IntuneLight.Models.Ise;
using IntuneLight.Models.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace IntuneLight.Services;

public interface IIseSessionService
{
    Task<IseSession?> GetSessionByMacAsync(string mac);
}

public sealed class IseSessionService(
    IHttpClientFactory httpClientFactory,
    IOptions<IseOptions> options,
    IApiResponseGuard guard) : IIseSessionService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IseOptions _options = options.Value;
    private readonly IApiResponseGuard _guard = guard;

    // Fetches ISE session data for a device MAC and determines exam/test mode.
    public async Task<IseSession?> GetSessionByMacAsync(string mac)
    {
        // Create named HttpClient for ISE API
        var client = _httpClientFactory.CreateClient("Ise");

        // Build Basic Auth header from configured credentials
        var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));

        // Format MAC address and build request URL
        var formattedMac = FormatMac(mac);
        var url = $"admin/API/mnt/Session/MACAddress/{Uri.EscapeDataString(formattedMac)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        // Send GET request to ISE MNT API
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Ensure success or treat no-data as valid (404/204)
        if (!_guard.EnsureSuccessOrNoData(response, SystemNames.IseSession, url, content))
        {
            return null;
        }

        // Ensure JSON body
        if (!_guard.EnsureJsonBody(content, SystemNames.IseSession, url, (int)response.StatusCode))
        {
            return null;
        }

        // Parse XML response and extract session details
        var doc = XDocument.Parse(content);
        var p = doc.Root;

        return new IseSession
        {
            Mode = ResolveMode(p?.Element("framed_ip_address")?.Value),
            IpAddress = p?.Element("framed_ip_address")?.Value,
            Vlan = p?.Element("vlan")?.Value,
            UserName = p?.Element("user_name")?.Value?.Split(",")[0],
            LastSeen = DateTime.TryParse(p?.Element("auth_acs_timestamp")?.Value, out var dt) ? dt : null,
            AznProfile = p?.Element("selected_azn_profiles")?.Value,
            RawXml = content
        };
    }

    // Resolves exam mode from IP address based on third octet ranges.
    private static ExamMode ResolveMode(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return ExamMode.Unknown;
        }

        var parts = ip.Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[2], out var octet))
        {
            return ExamMode.Normal;
        }

        if (octet >= 96 && octet <= 111)
        {
            return ExamMode.Exam;
        }
        else if (octet >= 120 && octet <= 127)
        {
            return ExamMode.Test;
        }
        else
        {
            return ExamMode.Normal;
        }
    }

    // Formats a MAC address to colon-separated format expected by ISE.
    private static string FormatMac(string mac)
    {
        var clean = mac.Replace(":", "").Replace("-", "");
        return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
    }
}
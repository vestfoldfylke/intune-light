using System.Text;
using System.Text.Json.Serialization;

namespace IntuneLight.Models.Intune;

public sealed class DeviceCredential
{
    [JsonPropertyName("@odata.context")] 
    public string? OdataContext { get; set; }

    public string? Id { get; set; }

    public string? DeviceName { get; set; }
        
    public DateTime? LastBackupDateTime { get; set; }

    public DateTime? RefreshDateTime { get; set; }

    public List<Credential>? Credentials { get; set; }

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = "";

    public void DecodeAllPasswords()
    {
        if (Credentials != null && Credentials.Count != 0)
        {
            foreach (var credential in Credentials)
            {
                if (!string.IsNullOrEmpty(credential.PasswordBase64))
                    credential.DecodedPassword = DecodeBase64(credential.PasswordBase64);
            }
        }
    }

    private static string DecodeBase64(string encodedString)
    {
        var decodedBytes = Convert.FromBase64String(encodedString);
        return Encoding.UTF8.GetString(decodedBytes);
    }
}

public class Credential
{
    public string? AccountName { get; set; }
    
    public string? AccountSid { get; set; }

    public DateTime? BackupDateTime { get; set; }

    public string? PasswordBase64 { get; set; }

    public string? DecodedPassword { get; set; }
    
}

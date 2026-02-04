namespace IntuneLight.Models.Intune;

public sealed class BitlockerRecoveryKeysResponse
{
    public List<BitlockerRecoveryKey> Value { get; set; } = [];
}

public sealed class BitlockerRecoveryKey
{
    public string Id { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public string? VolumeType { get; set; }
    public string? Key { get; set; }

    // Raw JSON payload for troubleshooting / raw viewer
    public string RawJson { get; set; } = "";
}

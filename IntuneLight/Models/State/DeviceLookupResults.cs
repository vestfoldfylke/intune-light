using IntuneLight.Models.Defender;
using IntuneLight.Models.Entra;
using IntuneLight.Models.Intune;
using IntuneLight.Models.Pureservice;

namespace IntuneLight.Models.State;

// DTO used to batch-apply lookup results to state.
public sealed class DeviceLookupResults
{
    public ManagedDevice? ManagedDevice { get; set; }
    public DefenderDevice? DefenderDevice { get; set; }
    public EntraUser? EntraUser { get; set; }
    public EntraDevice? EntraDevice { get; set; }
    public AutopilotDevice? AutopilotDevice { get; set; }
    public DeviceCredential? DeviceCredential { get; set; }
    public BitlockerRecoveryKey? BitlockerRecoveryKey { get; set; }
    public PureserviceUser? PureserviceUser { get; set; }
    public PureserviceTicket? PureserviceTicket { get; set; }
    public PureserviceAsset? PureserviceAssetBySn { get; set; }
    public PureserviceRelationshipSearchResponse? PureserviceRelationships { get; set; }
    public byte[]? EntraUserPhoto { get; set; }
    public int? UserDeviceCount { get; set; }
    public bool IsIsolated { get; set; }
}
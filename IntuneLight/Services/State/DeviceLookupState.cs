using IntuneLight.Models.Defender;
using IntuneLight.Models.Entra;
using IntuneLight.Models.Intune;
using IntuneLight.Models.Pureservice;
using IntuneLight.Models.State;

namespace IntuneLight.Services.State;

// Holds the current device lookup context and results across pages/components.
public sealed class DeviceLookupState
{
    private bool _isDarkMode;
    private bool _isFetching;
    private bool _hasSearched;
    private string _searchSerial = string.Empty;
    private bool _isIsolated;

    public event Action? StateChanged;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set { if (_isDarkMode == value) return; _isDarkMode = value; NotifyStateChanged(); }
    }

    public bool IsFetching
    {
        get => _isFetching;
        set { if (_isFetching == value) return; _isFetching = value; NotifyStateChanged(); }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set { if (_hasSearched == value) return; _hasSearched = value; NotifyStateChanged(); }
    }

    public string SearchSerial
    {
        get => _searchSerial;
        set { if (_searchSerial == value) return; _searchSerial = value.Trim(); NotifyStateChanged(); }
    }

    public bool IsIsolated
    {
        get => _isIsolated;
        set { if (_isIsolated == value) return; _isIsolated = value; NotifyStateChanged(); }
    }

    // Results
    public ManagedDevice? ManagedDevice { get; private set; }
    public DefenderDevice? DefenderDevice { get; private set; }
    public EntraUser? EntraUser { get; private set; }
    public EntraDevice? EntraDevice { get; private set; }
    public AutopilotDevice? AutopilotDevice { get; private set; }
    public DeviceCredential? DeviceCredential { get; private set; }
    public BitlockerRecoveryKey? BitlockerRecoveryKey { get; private set; }
    public PureserviceUser? PureserviceUser { get; private set; }
    public PureserviceTicket? PureserviceTicket { get; private set; }
    public PureserviceAsset? PureserviceAssetBySn { get; private set; }
    public PureserviceRelationshipSearchResponse? PureserviceRelationships { get; private set; }
    public string PureserviceTicketAddress { get; private set; } = string.Empty;
    public byte[]? EntraUserPhoto { get; private set; }
    public int? UserDeviceCount { get; set; }
    public string? ClientIpAddress { get; set; }

    // Clears all results for a new lookup while keeping UI flags optional.
    public void ClearResults(bool keepSearchSerial = true)
    {
        if (!keepSearchSerial)
            _searchSerial = string.Empty;

        ManagedDevice = null;
        DefenderDevice = null;
        EntraUser = null;
        EntraDevice = null;
        AutopilotDevice = null;
        DeviceCredential = null;
        BitlockerRecoveryKey = null;
        PureserviceUser = null;
        PureserviceTicket = null;
        PureserviceAssetBySn = null;
        PureserviceRelationships = null;
        EntraUserPhoto = null;
        UserDeviceCount = null;
        IsIsolated = false;

        NotifyStateChanged();
    }

    // Sets all lookup results in one call to avoid UI repaint spam.
    public void SetResults(DeviceLookupResults results)
    {
        ManagedDevice = results.ManagedDevice;
        DefenderDevice = results.DefenderDevice;
        EntraUser = results.EntraUser;
        EntraDevice = results.EntraDevice;
        AutopilotDevice = results.AutopilotDevice;
        DeviceCredential = results.DeviceCredential;
        BitlockerRecoveryKey = results.BitlockerRecoveryKey;
        PureserviceUser = results.PureserviceUser;
        PureserviceTicket = results.PureserviceTicket;
        PureserviceAssetBySn = results.PureserviceAssetBySn;
        PureserviceRelationships = results.PureserviceRelationships;
        EntraUserPhoto = results.EntraUserPhoto;
        UserDeviceCount = results.UserDeviceCount;
        IsIsolated = results.IsIsolated;

        NotifyStateChanged();
    }

    public void SetPureserviceTicketAddress(string address)
    {
        PureserviceTicketAddress = address ?? string.Empty;
        NotifyStateChanged();
    }

    public void SetClientIpAddress(string address)
    {
        ClientIpAddress = address ?? string.Empty;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}

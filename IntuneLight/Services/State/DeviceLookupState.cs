using IntuneLight.Infrastructure;
using IntuneLight.Models.Defender;
using IntuneLight.Models.Entra;
using IntuneLight.Models.Intune;
using IntuneLight.Models.Pureservice;
using IntuneLight.Models.State;

namespace IntuneLight.Services.State;

// Holds the current device lookup context and results across pages/components.
public sealed class DeviceLookupState
{
    #region Props

    private bool _isDarkMode;
    private bool _isFetching;
    private bool _hasSearched;
    private string _searchSerial = string.Empty;
    private bool _isIsolated;

    public event Action? StateChanged;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set 
        { 
            if (_isDarkMode == value) 
            {
                return; 
            }
            _isDarkMode = value; NotifyStateChanged(); 
        }
    }

    public bool IsFetching
    {
        get => _isFetching;
        set 
        {
            if (_isFetching == value)
            {
                return;
            }
            _isFetching = value; 
            NotifyStateChanged(); 
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set 
        { 
            if (_hasSearched == value) 
            {
                return; 
            }
            _hasSearched = value; 
            NotifyStateChanged(); }
    }

    public string SearchSerial
    {
        get => _searchSerial;
        set
        {
            var normalized = NormalizeSerial(value);
            if (_searchSerial == normalized)
            {
                return;
            }

            _searchSerial = normalized;
            IsSearchSerialTouched = true;
            SearchSerialError = GetSerialError(_searchSerial);
            NotifyStateChanged();
        }
    }

    public bool IsIsolated
    {
        get => _isIsolated;
        set 
        { 
            if (_isIsolated == value) 
            { 
                return; 
            }
            
            _isIsolated = value; 
            NotifyStateChanged(); }
    }

    // Results
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
    public string PureserviceTicketAddress { get; set; } = string.Empty;
    public byte[]? EntraUserPhoto { get; set; }
    public int? UserDeviceCount { get; set; }
    public string? ClientIpAddress { get; set; }
    public EntraDeviceCount?  EntraDeviceCount { get; set; }
    public HashSet<string> LapsRotationLockedDevices { get; } = [];
    public List<PureserviceAssetStatus>? AssetStatuses { get; set; }

    #endregion

    #region Clear

    // Clears all results for a new lookup while keeping UI flags optional.
    public void ClearResults(bool keepSearchSerial = true)
    {
        if (!keepSearchSerial)
        {
            _searchSerial = string.Empty;
        }

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
        EntraDeviceCount = null;
        IsSearchSerialTouched = false;
        HasSearched = false;
        AssetStatuses = null;
        NotifyStateChanged();
    }

    #endregion

    #region Set

    // Sets all lookup results in one call to avoid UI repaint spam.
    public void SetResults(DeviceLookupResults results)
    {
        ManagedDevice = results.ManagedDevice;
        DefenderDevice = results.DefenderDevice;
        EntraUser = results.EntraUser;
        EntraDevice = results.EntraDevice;
        AutopilotDevice = results.AutopilotDevice;
        PureserviceUser = results.PureserviceUser;
        PureserviceTicket = results.PureserviceTicket;
        PureserviceAssetBySn = results.PureserviceAssetBySn;
        PureserviceRelationships = results.PureserviceRelationships;
        EntraUserPhoto = results.EntraUserPhoto;
        UserDeviceCount = results.UserDeviceCount;
        IsIsolated = results.IsIsolated;
        EntraDeviceCount = results.EntraDeviceCount;
        AssetStatuses = results.AssetStatuses;
        NotifyStateChanged();
    }

    #endregion

    #region State changed
    private void NotifyStateChanged() => StateChanged?.Invoke();
    public void Touch() => NotifyStateChanged();

    #endregion

    #region Validation

    public string SearchSerialError { get; private set; } = GetSerialError(string.Empty);
    public bool IsSearchSerialValid => string.IsNullOrEmpty(SearchSerialError);
    public bool IsSearchSerialTouched { get; private set; } = false;

    // Normalizes a serial number for lookup — trims, removes whitespace, uppercases, and escapes OData injection characters.
    private static string NormalizeSerial(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input.Trim()
                    .Replace(" ", string.Empty)
                    .Replace("\t", string.Empty)
                    .ToUpperInvariant()
                    .Replace("'", "''");
    }

    // Validates serial number format — allows letters, digits, and hyphens within expected length bounds.
    private static string GetSerialError(string serial)
    {
        // Normalize before validation so length check reflects actual lookup value
        serial = NormalizeSerial(serial);

        if (string.IsNullOrEmpty(serial))
        {
            return "Serienummer mangler.";
        }
        else if (serial.Length < 7)
        {
            return "Serienummer er for kort.";
        }
        else if (serial.Length > 40)
        {
            return "Serienummer er for langt.";
        }
        else if (serial.All(c => c == '-'))
        {
            return "Serienummer er ugyldig.";
        }

        foreach (var c in serial)
        {
            if (!(char.IsLetterOrDigit(c) || c == '-'))
            {
                return "Serienummer kan kun inneholde bokstaver, tall og bindestrek.";
            }
        }

        return string.Empty;
    }

    #endregion

    #region Audit

    // Builds an AuditContext from current device lookup state.
    public AuditContext BuildAuditContext() => new()
    {
        DeviceId = ManagedDevice?.AzureADDeviceId,
        DeviceName = ManagedDevice?.DeviceName
            ?? DefenderDevice?.ComputerDnsName
            ?? AutopilotDevice?.SerialNumber,
        DeviceOwner = EntraUser?.UserPrincipalName
            ?? EntraUser?.DisplayName
            ?? "Ukjent"
    };

    #endregion
}
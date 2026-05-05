using IntuneLight.Components.Dialogs;
using IntuneLight.Helpers;
using IntuneLight.Infrastructure;
using IntuneLight.Models.ApiError;
using IntuneLight.Models.Defender;
using IntuneLight.Models.Offboarding;
using IntuneLight.Models.Pureservice;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace IntuneLight.Components.Pages;


public partial class Home : ComponentBase
{
    #region Entra device

    // Deletes the selected Entra device after explicit user confirmation.
    private async Task DeleteEntraDeviceAsync()
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Entra-enhet",
            message: "Du er i ferd med å slette en Entra-enhet. Enheten fjernes <b>permanent</b> fra Entra ID.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
        {
            return;
        }

        var deviceId = _state.ManagedDevice.AzureADDeviceId;

        await _uiErrorHandler.RunSafeAsync(
            async () =>
            {
                // Check if the device is still present in Autopilot before attempting deletion.
                var autopilotDevice = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.ManagedDevice.SerialNumber);
                if (autopilotDevice is not null)
                {
                    throw new UiValidationException(
                        systemName: SystemNames.EntraDeviceDelete,
                        message: "Enheten ligger i Autopilot og kan ikke slettes.");
                }

                // Delete the device from Entra ID using the Azure AD device ID.
                await _entraDirectoryService.DeleteDeviceByAzureAdDeviceIdAsync(deviceId, _state.BuildAuditContext());

                _snackbar.Add("Enheten ble slettet.", Severity.Success);

                // Clear the selected device from the state after deletion.
                _state.EntraDevice = null;
                _state.Touch();
            });
    }

    #endregion

    #region Intune device

    // Triggers a sync for the currently selected Intune managed device.
    private async Task SyncIntuneDeviceAsync()
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(
            async () =>
            {
                await _intuneService.SyncManagedDeviceAsync(_state.ManagedDevice.Id);
                _snackbar.Add("Synkronisering er sendt til enheten.", Severity.Success);
            });
    }

    // Wipes the selected Intune device after explicit user confirmation.
    private async Task WipeIntuneDeviceAsync()
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Wipe enhet",
            message: "Du er i ferd med å wipe enheten. Dette sletter data og kan ikke angres.",
            confirmText: "Wipe",
            cancelText: "Avbryt");

        if (!confirmed)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.WipeManagedDeviceAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());
            _snackbar.Add("Wipe-kommando er sendt til enheten.", Severity.Success);
        });
    }

    // Requests remote assistance for the selected Intune device.
    private async Task RequestRemoteAssistanceAsync()
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.RequestRemoteAssistanceAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());
            _snackbar.Add("Fjernhjelp-forespørsel er sendt.", Severity.Success);
        });
    }

    // Deletes the selected Intune managed device record after explicit user confirmation.
    private async Task DeleteIntuneDeviceAsync()
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Intune-enhet",
            message: "Du er i ferd med å slette Intune-enheten. Dette fjerner enheten fra Intune og kan ikke angres.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.DeleteManagedDeviceAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());

            _snackbar.Add("Enheten ble slettet fra Intune.", Severity.Success);
            _state.ManagedDevice = null;
            _state.Touch();
        });
    }

    #endregion

    #region Defender device

    // Runs a Defender antivirus scan (quick/full) for the currently selected device.
    private async Task RunDefenderScanAsync(DefenderScanType scanType)
    {
        if (_state.DefenderDevice is null)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            var result = await _defenderService.RunAntiVirusScanAsync(_state.DefenderDevice.Id, scanType, _state.BuildAuditContext());

            if (result == DefenderScanResult.Started)
            {
                var label = scanType == DefenderScanType.Full ? "Full skann" : "Hurtigskann";
                _snackbar.Add($"{label} er sendt til Microsoft Defender. Skanningen kjøres når enheten er klar.", Severity.Success);
                
                return;
            }

            _snackbar.Add("En skann-forespørsel er allerede sendt for enheten (venter eller pågår). Prøv igjen når den er ferdig.", 
                          Severity.Info, conf => conf.RequireInteraction = true);
        });
    }

    #endregion

    #region Autopilot device

    // Deletes the selected Autopilot device entry after explicit user confirmation.
    private async Task DeleteAutopilotDeviceAsync()
    {
        if (_state.AutopilotDevice is null)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Autopilot-oppføring",
            message: "Du er i ferd med å slette en Autopilot-oppføring. Dette kan ikke angres.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.DeleteAutopilotDeviceAsync(_state.AutopilotDevice.Id, _state.BuildAuditContext());

            _snackbar.Add("Autopilot-oppføringen ble slettet.", Severity.Success);
            _state.AutopilotDevice = null;
            _state.Touch();
        });
    }

    // Opens a dialog to set or update the Autopilot tag for the device, and updates it.
    private async Task SetAutopilotTagAsync()
    {
        if (_state.AutopilotDevice is null)
        {
            return;
        }

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            BackdropClick = false
        };

        var dialog = await _dialogService.ShowAsync<AutopilotTagDialog>("Endre Autopilot-tag", options);
        var result = await dialog.Result;

        if (result != null && result.Canceled || result?.Data is not string tag || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        // No-op if unchanged
        if (string.Equals(_state.AutopilotDevice.GroupTag ?? string.Empty, tag, StringComparison.Ordinal))
        {
            _snackbar.Add("Valgt tag er allerede satt på enheten.", Severity.Info);
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.UpdateAutopilotGroupTagAsync(_state.AutopilotDevice.Id, tag);
            _snackbar.Add("Autopilot-tag er sendt til Intune.", Severity.Success);
        });
    }

    #endregion

    #region LAPS

    // Rotates the LAPS local admin password for the selected Intune device.
    private async Task RotateLapsPasswordAsync()
    {
        if (_state.ManagedDevice is null)
        {  
            return; 
        }

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Roter LAPS-passord",
            message:
                @"Du er i ferd med å rotere LAPS-passordet. Det nye passordet blir tilgjengelig når enheten har fullført rotasjonen og rapportert inn til Intune.

                <strong>Merk:</strong> Dette kan ta tid (fra noen minutter til over 30 minutter). 
                
                «Oppdater» viser først nytt passord når enheten har meldt det inn.",
            confirmText: "Roter",
            cancelText: "Avbryt",
            isIrreversible: false);

        if (!confirmed)
        {
            return;
        }

        await _uiErrorHandler.RunSafeAsync(async () =>
        {
            await _intuneService.RotateLocalAdminPasswordAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());

            // Allow only one rotation at a time per device.
            _state.LapsRotationLockedDevices.Add(_state.ManagedDevice.Id);
            _state.Touch();

            _snackbar.Add("LAPS-rotasjon er sendt til Intune. Enheten vil rotere passord ved neste check-in. Du kan kjøre Sync for å fremskynde.", Severity.Success);
        });
    }

    #endregion

    #region UI refresh (refresh data from APIs)

    // Refreshes data for a specific card only.
    private async Task RefreshAsync(RefreshTarget target)
    {
        if (_state.ManagedDevice is null)
        {
            return;
        }

        // Set loading target for spinner
        _loadingTarget = target;

        try
        {
            await _uiErrorHandler.RunSafeAsync(async () =>
            {
                switch (target)
                {
                    case RefreshTarget.Intune:
                        _state.ManagedDevice = await _intuneService.GetDeviceBySerialAsync(_state.SearchSerial);
                        break;

                    case RefreshTarget.Autopilot:
                        _state.AutopilotDevice = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.ManagedDevice.SerialNumber);
                        break;

                    case RefreshTarget.Defender:
                        _state.DefenderDevice = await _defenderService.GetDeviceByAadDeviceIdAsync(_state.ManagedDevice.AzureADDeviceId);

                        if (_state.DefenderDevice is not null)
                            _state.IsIsolated = await _defenderService.GetIsolationStatusByMachineId(_state.DefenderDevice.Id);
                        else
                            _state.IsIsolated = false;

                        break;

                    case RefreshTarget.Entra:
                        _state.EntraDevice = await _entraDirectoryService.GetDeviceByAzureAdDeviceIdAsync(_state.ManagedDevice.AzureADDeviceId);
                        break;

                    case RefreshTarget.Pureservice:
                        _state.PureserviceAssetBySn = await _pureserviceService.GetAssetBySerialAsync(_state.ManagedDevice.SerialNumber);

                        if (_state.PureserviceAssetBySn is not null)
                            _state.PureserviceRelationships = await _pureserviceService.GetRelationshipsByAssetIdAsync(_state.PureserviceAssetBySn.Id.ToString());
                        else
                            _state.PureserviceRelationships = null;

                        break;

                    case RefreshTarget.Laps:
                        _state.DeviceCredential = await _intuneService.GetLapsPasswordByAzureDeviceId(
                            _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());
                        break;

                    case RefreshTarget.BitLocker:
                        _state.BitlockerRecoveryKey = await _intuneService.GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(
                            _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());
                        break;

                    case RefreshTarget.IseSession:
                        var mac = _state.ManagedDevice.WiFiMacAddress ?? _state.ManagedDevice.EthernetMacAddress;

                        if (mac is null)
                        {
                            _snackbar.Add("Ingen MAC-adresse funnet på enheten.", Severity.Warning);
                            return;
                        }

                        _state.IseSession = await _iseSessionService.GetSessionByMacAsync(mac);
                        break;
                }
            });
        }
        finally
        {

            // Delay to give user action performed signal
            await Task.Delay(300);

            // Stop loading
            _loadingTarget = null;

            _state.Touch();
        }
    }

    #endregion

    #region Enums

    public enum RefreshTarget
    {
        Intune,
        Autopilot,
        Defender,
        Entra,
        Pureservice,
        Laps,
        BitLocker,
        IseSession
    }

    #endregion

    #region Offboarding

    // Offboarding options injected from configuration, containing settings like asset status IDs and feature flags.
    [Inject] private IOptions<PureserviceOffboardingOptions> OffboardingOptions { get; set; } = default!;

    // Temporary storage for the LAPS password and BitLocker key during the offboarding process
    private string? _lapsPassword;
    private string? _bitlockerKey;

    // List to track the results of each offboarding step for display in the offboarding overlay.
    private List<OffboardingStepResult> _offboardingSteps = [];

    // Flag to indicate whether the offboarding process is currently running, used to control UI state and behavior.
    private bool _isOffboarding = false;

    // Cancellation token source to allow cancelling the offboarding process if needed
    private CancellationTokenSource? _offboardingCts;

    // Cancels the offboarding process if it's currently running.
    private void CancelOffboardingAsync() => _offboardingCts?.Cancel();

    // Starts the offboarding process for the selected device, performing multiple steps across different systems with error handling and user feedback.
    private async Task StartOffboardingAsync()
    {
        // Offboarding is only available if we have a managed device
        if (_state.ManagedDevice is null)
        {
            return;
        }

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = false,
            BackdropClick = false,
            CloseOnEscapeKey = false
        };

        // Determine if the device is sold or offboardable based on its current status in Pureservice.
        var currentStatusId = _state.PureserviceAssetBySn?.StatusId;
        var soldStatus = _state.AssetStatuses?.FirstOrDefault(s => s.Name == PureserviceNames.AssetStatusSold);
        var isSold = currentStatusId.HasValue && soldStatus?.Id == currentStatusId;

        // Offboarding is allowed if the asset status is one of the defined offboarding statuses and matches the current status of the asset.
        var offboardingNames = new[]
        {
            PureserviceNames.AssetStatusDiscarded,
            PureserviceNames.AssetStatusShared,
            PureserviceNames.AssetStatusLost,
            PureserviceNames.AssetStatusStolen,
            PureserviceNames.AssetStatusDiscardedRedistributed
        };

        // Offboarding is allowed if the asset status is one of the defined offboarding statuses and matches the current status of the asset.
        var isOffboardable = currentStatusId.HasValue && (_state.AssetStatuses?.Any(s =>
            offboardingNames.Contains(s.Name) && s.Id == currentStatusId) ?? false);

        var parameters = new DialogParameters
        {
            ["IsAssetSold"] = isSold,
            ["IsOffboardable"] = isOffboardable,
            ["HasPureserviceTicket"] = _state.PureserviceTicket is not null
        };

        var dialog = await _dialogService.ShowAsync<OffboardingConfirmDialog>("", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        // Extract the user's offboarding selections from the dialog result to determine which steps to perform.
        var selection = (OffboardingSelection)result.Data!;
        var withWipe = selection.Method == OffboardingMethod.WithWipe;
        var isForceDelete = selection.Method == OffboardingMethod.ForceDelete;
        var isPrivatization = selection.Routine == OffboardingRoutine.Privatization;

        // Reset offboarding state and create cancellation token
        _offboardingCts = new CancellationTokenSource();
        var ct = _offboardingCts.Token;
        _isOffboarding = true;
        _lapsPassword = null;
        _bitlockerKey = null;
        _stepSeconds = [];

        // Build step list dynamically based on selected method
        var steps = new List<OffboardingStepResult>();

        if (!isForceDelete)
        {
            steps.Add(new("Henter LAPS-passord", OffboardingStepStatus.Pending));
            steps.Add(new("Henter BitLocker-nøkkel", OffboardingStepStatus.Pending));
        }

        steps.Add(new("Oppretter sak i Pureservice", OffboardingStepStatus.Pending));
        steps.Add(new("Fjerner fra Autopilot", OffboardingStepStatus.Pending));

        if (withWipe)
        {
            steps.Add(new("Synkroniserer Intune", OffboardingStepStatus.Pending));
            steps.Add(new("Wiper enhet", OffboardingStepStatus.Pending));
        }

        if (withWipe || isForceDelete)
        {
            steps.Add(new("Fjerner fra Entra", OffboardingStepStatus.Pending));
            steps.Add(new("Fjerner fra Intune", OffboardingStepStatus.Pending));
        }

        steps.Add(new("Tagger i Defender", OffboardingStepStatus.Pending));

        _offboardingSteps = steps;
        StateHasChanged();

        // Step 1 & 2: Fetch LAPS and BitLocker - skipped for force delete
        if (!isForceDelete)
        {
            await RunOffboardingStepAsync("Henter LAPS-passord", async () =>
            {
                var credential = await _intuneService.GetLapsPasswordByAzureDeviceId(
                    _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());

                if (credential?.Credentials is { Count: > 0 })
                {
                    _lapsPassword = credential.Credentials.First().DecodedPassword;
                }
            }, ct);

            await RunOffboardingStepAsync("Henter BitLocker-nøkkel", async () =>
            {
                var key = await _intuneService.GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(
                    _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());

                if (key?.Key is not null)
                {
                    _bitlockerKey = key.Key;
                }
            }, ct);
        }

        // Step 3: Create ticket in Pureservice
        if (_state.PureserviceAssetBySn is null)
        {
            UpdateStep("Oppretter sak i Pureservice", OffboardingStepStatus.Skipped, "Mangler asset i Pureservice");
        }
        else if (isPrivatization && _state.PureserviceUser is null)
        {
            UpdateStep("Oppretter sak i Pureservice", OffboardingStepStatus.Skipped, "Mangler bruker i Pureservice");
        }
        else
        {
            await RunOffboardingStepAsync("Oppretter sak i Pureservice", async () =>
            {
                var subject = isForceDelete
                    ? "Tvangssletting av pc"
                    : isPrivatization ? "Privatisering av pc" : "Sletting av pc";

                var description = isForceDelete
                    ? $"""
                    <p>Maskinen tvangsslettes.</p>
                    <p>Behandlet av: {UserCtx.Upn}</p>
                    """
                    : $"""
                    <p>Maskinen skal {(isPrivatization ? "privatiseres" : "slettes")}.</p>
                    <p>Behandlet av: {UserCtx.Upn}</p>
                    <p>LAPS-passord: {_lapsPassword ?? "ikke tilgjengelig"}</p>
                    <p>BitLocker-nøkkel: {_bitlockerKey ?? "ikke tilgjengelig"}</p>
                    """;

                var effectiveUserId = isPrivatization
                    ? _state.PureserviceUser!.Id
                    : OffboardingOptions.Value.AgentId;

                await _pureserviceService.CreateOffboardingTicketAsync(
                    subject: subject,
                    description: description,
                    userId: effectiveUserId,
                    assetId: _state.PureserviceAssetBySn.Id,
                    routine: isPrivatization ? OffboardingRoutine.Privatization : OffboardingRoutine.Deletion,
                    userUpn: _state.EntraUser?.UserPrincipalName);
            }, ct);
        }

        // Step 4: Remove from Autopilot - skip if already gone
        if (_state.AutopilotDevice is null)
        {
            UpdateStep("Fjerner fra Autopilot", OffboardingStepStatus.Skipped, "Ikke funnet");
        }
        else
        {
            await RunOffboardingStepAsync("Fjerner fra Autopilot", async () =>
            {
                // Check if already deleted before attempting
                var existing = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.SearchSerial);
                if (existing is null)
                {
                    return; // Already gone, skip silently
                }

                await _intuneService.DeleteAutopilotDeviceAsync(_state.AutopilotDevice.Id,
                      _state.BuildAuditContext());
            }, ct);
        }

        // Step 5 & 6: Sync and wipe - only for wipe method
        if (withWipe)
        {
            await RunOffboardingStepAsync("Synkroniserer Intune", async () =>
            {
                var syncBefore = _state.ManagedDevice.LastSyncDateTime;
                await _intuneService.SyncManagedDeviceAsync(_state.ManagedDevice.Id);

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(10000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);
                    if (device?.LastSyncDateTime > syncBefore)
                    {
                        break;
                    }
                }
            }, ct);

            await RunOffboardingStepAsync("Wiper enhet", async () =>
            {
                await _intuneService.WipeManagedDeviceAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);
                    var wipeAction = device?.DeviceActionResults?.FirstOrDefault(a => a.ActionName == "wipe");
                    if (wipeAction?.ActionState is "pending" or "active" or "done")
                    {
                        break;
                    }
                }

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);

                    if (device is null || device.ManagementState == "wipePending")
                    {
                        break;
                    }

                    if (device.ManagementState is "wipeFailed" or "wipeCanceled")
                    {
                        throw new InvalidOperationException($"Wipe feilet med status: {device.ManagementState}.");
                    }
                }
            }, ct);
        }

        // Step 7: Remove from Entra - wipe and force delete
        if (withWipe || isForceDelete)
        {
            if (_state.EntraDevice is null)
            {
                UpdateStep("Fjerner fra Entra", OffboardingStepStatus.Skipped, "Ikke funnet");
            }
            else
            {
                await RunOffboardingStepAsync("Fjerner fra Entra", async () =>
                {
                    await _entraDirectoryService.DeleteDeviceByAzureAdDeviceIdAsync(
                        _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());
                }, ct);
            }
        }

        // Step 8: Remove from Intune
        if (withWipe)
        {
            var wipeStep = _offboardingSteps.FirstOrDefault(s => s.StepName == "Wiper enhet");
            var wipeSuccess = wipeStep?.Status == OffboardingStepStatus.Success;

            UpdateStep("Fjerner fra Intune",
                wipeSuccess ? OffboardingStepStatus.Success : OffboardingStepStatus.Skipped,
                ct.IsCancellationRequested ? "Avbrutt av bruker" :
                wipeSuccess ? "Fjernet av wipe" : "Ikke utført");
        }
        else if (isForceDelete)
        {
            await RunOffboardingStepAsync("Fjerner fra Intune", async () =>
            {
                await _intuneService.DeleteManagedDeviceAsync(_state.ManagedDevice.Id, _state.BuildAuditContext());
            }, ct);
        }

        // Step 9: Tag Defender
        if (_state.DefenderDevice is null)
        {
            UpdateStep("Tagger i Defender", OffboardingStepStatus.Skipped, "Ikke funnet");
        }
        else
        {
            await RunOffboardingStepAsync("Tagger i Defender", async () =>
            {
                await _defenderService.AddMachineTagAsync(_state.DefenderDevice.Id, DefenderTags.Offboarded);

                if (isPrivatization)
                {
                    await _defenderService.AddMachineTagAsync(_state.DefenderDevice.Id, DefenderTags.Privatized);
                }
            }, ct);
        }

        _isOffboarding = false;
        _offboardingCts = null;
        StateHasChanged();
    }

    // Helper method to run each offboarding step with error handling, result tracking and live timer.
    private Dictionary<string, int> _stepSeconds = [];
    private async Task RunOffboardingStepAsync(string stepName, Func<Task> action, CancellationToken ct)
    {
        // Stop if a previous step has failed
        if (_offboardingSteps.Any(s => s.Status == OffboardingStepStatus.Failed))
        {
            return;
        }

        // Stop if cancellation was requested
        if (ct.IsCancellationRequested)
        {
            UpdateStep(stepName, OffboardingStepStatus.Skipped, "Avbrutt av bruker");
            return;
        }

        // Set current step to Running and start live timer
        UpdateStep(stepName, OffboardingStepStatus.Running);
        _stepSeconds[stepName] = 0;
        StateHasChanged();

        // Background task that increments the step timer every second
        using var timerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timerTask = Task.Run(async () =>
        {
            while (!timerCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, timerCts.Token);
                    _stepSeconds[stepName]++;
                    await InvokeAsync(StateHasChanged);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, timerCts.Token);

        try
        {
            await action();
            timerCts.Cancel();
            await timerTask;
            UpdateStep(stepName, OffboardingStepStatus.Success);
        }
        catch (OperationCanceledException)
        {
            timerCts.Cancel();
            await timerTask;
            UpdateStep(stepName, OffboardingStepStatus.Skipped, "Avbrutt av bruker");
        }
        catch (ApiException apiEx)
        {
            timerCts.Cancel();
            await timerTask;
            UpdateStep(stepName, OffboardingStepStatus.Failed, "Feilet");
            await _uiErrorHandler.HandleAsync(apiEx);
        }
        catch (Exception ex)
        {
            timerCts.Cancel();
            await timerTask;
            UpdateStep(stepName, OffboardingStepStatus.Failed, "Feilet");
            await _uiErrorHandler.HandleAsync(ex);
        }

        StateHasChanged();
    }

    // Updates an existing step result by name.
    private void UpdateStep(string stepName, OffboardingStepStatus status, string? message = null)
    {
        var index = _offboardingSteps.FindIndex(s => s.StepName == stepName);
        if (index >= 0)
        {
            _offboardingSteps[index] = new OffboardingStepResult(stepName, status, message);
        }
    }

    // Resets the offboarding state to allow starting a new offboarding process.
    private void ResetOffboarding()
    {
        _offboardingSteps = [];
        _lapsPassword = null;
        _bitlockerKey = null;
        _stepSeconds = [];
        _state.ClearResults();
        StateHasChanged();
    }

    /// <summary>Returns the offboarding status title based on current step results.</summary>
    private string GetOffboardingTitle()
    {
        if (_isOffboarding)
        {
            return "Offboarding pågår";
        }
        else if (_offboardingSteps.Any(s => s.Status == OffboardingStepStatus.Failed))
        {
            return "Offboarding feilet";
        }
        else if (_offboardingSteps.Any(s => s.Status == OffboardingStepStatus.Skipped && s.Message == "Avbrutt av bruker"))
        {
            return "Offboarding avbrutt";
        }
        else 
        {
            return "Offboarding fullført";
        }
    }

    #endregion
}
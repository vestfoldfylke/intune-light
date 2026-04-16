using IntuneLight.Components.Dialogs;
using IntuneLight.Helpers;
using IntuneLight.Infrastructure;
using IntuneLight.Models.ApiError;
using IntuneLight.Models.Defender;
using IntuneLight.Models.Offboarding;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IntuneLight.Components.Pages;


public partial class Home : ComponentBase
{
    #region Entra device

    // Deletes the selected Entra device after explicit user confirmation.
    private async Task DeleteEntraDeviceAsync()
    {
        if (_state.ManagedDevice is null)
            return;

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Entra-enhet",
            message: "Du er i ferd med å slette en Entra-enhet. Enheten fjernes <b>permanent</b> fra Entra ID.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
            return;

        var deviceId = _state.ManagedDevice.AzureADDeviceId;

        await _uiErrorHandler.RunSafeAsync(
            async () =>
            {
                // Check if the device is still present in Autopilot before attempting deletion.
                var autopilotDevice = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.ManagedDevice.SerialNumber);
                if (autopilotDevice is not null)
                    throw new UiValidationException(
                        systemName: SystemNames.EntraDeviceDelete,
                        message: "Enheten ligger i Autopilot og kan ikke slettes.");

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
            return;

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
            return;

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Wipe enhet",
            message: "Du er i ferd med å wipe enheten. Dette sletter data og kan ikke angres.",
            confirmText: "Wipe",
            cancelText: "Avbryt");

        if (!confirmed)
            return;

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
            return;

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
            return;

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Intune-enhet",
            message: "Du er i ferd med å slette Intune-enheten. Dette fjerner enheten fra Intune og kan ikke angres.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
            return;

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
            return;

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
            return;

        var confirmed = await _dialogService.ConfirmIrreversibleAsync(
            title: "Slett Autopilot-oppføring",
            message: "Du er i ferd med å slette en Autopilot-oppføring. Dette kan ikke angres.",
            confirmText: "Slett",
            cancelText: "Avbryt");

        if (!confirmed)
            return;

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
            return;

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
            return;

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
            return;

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
            return;

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
            return;

        // Set loading target for spinner
        _loadingTarget = target;

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
            }

            // Delay to give user action performed signal
            await Task.Delay(300);

            // Stop loading
            _loadingTarget = null;

            _state.Touch();
        });
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
        BitLocker
    }

    #endregion

    #region Offboarding

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
            return;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = false,
            BackdropClick = false
        };

        // Show confirmation dialog with option to wipe or not.
        var dialog = await _dialogService.ShowAsync<OffboardingConfirmDialog>("", options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        var offboardingType = (OffboardingType)result.Data!;
        var withWipe = offboardingType == OffboardingType.WithWipe;

        // Reset offboarding state and create cancellation token
        _offboardingCts = new CancellationTokenSource();
        var ct = _offboardingCts.Token;
        _isOffboarding = true;
        _lapsPassword = null;
        _stepSeconds = [];

        _offboardingSteps = [
            new("Henter LAPS-passord", OffboardingStepStatus.Pending),
            new("Henter BitLocker-nøkkel", OffboardingStepStatus.Pending),
            new("Fjerner fra Autopilot", OffboardingStepStatus.Pending),
            new("Synkroniserer Intune", OffboardingStepStatus.Pending),
            new("Wiper enhet", OffboardingStepStatus.Pending),
            new("Fjerner fra Entra", OffboardingStepStatus.Pending),
            new("Fjerner fra Intune", OffboardingStepStatus.Pending),
            new("Tagger i Defender", OffboardingStepStatus.Pending),
            new("Oppdaterer status i Pureservice", OffboardingStepStatus.Pending),
            new("Oppretter sak i Pureservice", OffboardingStepStatus.Pending),
        ];

        StateHasChanged();

        // Step 0: Fetch LAPS password before offboarding
        await RunOffboardingStepAsync("Henter LAPS-passord", async () =>
        {
            var credential = await _intuneService.GetLapsPasswordByAzureDeviceId(
                _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());

            if (credential?.Credentials is { Count: > 0 })
                _lapsPassword = credential.Credentials.First().DecodedPassword;
        }, ct);

        // Step 1: Fetch BitLocker key before offboarding
        await RunOffboardingStepAsync("Henter BitLocker-nøkkel", async () =>
        {
            var key = await _intuneService.GetBitlockerRecoveryKeyByAzureAdDeviceIdAsync(
                _state.ManagedDevice.AzureADDeviceId, _state.BuildAuditContext());

            if (key?.Key is not null)
                _bitlockerKey = key.Key;
        }, ct);

        // Step 2: Remove from Autopilot - skip if already gone
        if (_state.AutopilotDevice is null)
            UpdateStep("Fjerner fra Autopilot", OffboardingStepStatus.Skipped, "Ikke funnet");
        else
            await RunOffboardingStepAsync("Fjerner fra Autopilot", async () =>
            {
                // Check if already deleted before attempting
                var existing = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.SearchSerial);
                if (existing is null)
                    return; // Already gone, skip silently

                await _intuneService.DeleteAutopilotDeviceAsync(_state.AutopilotDevice.Id, _state.BuildAuditContext());

                // Poll until removed
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    var device = await _intuneService.GetAutopilotDeviceBySerialAsync(_state.SearchSerial);
                    if (device is null)
                        break;
                }
            }, ct);

        // Step 3: Sync Intune - wait until lastSyncDateTime changes (only with wipe)
        if (!withWipe)
            UpdateStep("Synkroniserer Intune", OffboardingStepStatus.Skipped, "Ikke valgt");
        else
            await RunOffboardingStepAsync("Synkroniserer Intune", async () =>
            {
                var syncBefore = _state.ManagedDevice.LastSyncDateTime;
                await _intuneService.SyncManagedDeviceAsync(_state.ManagedDevice.Id);

                // Poll until lastSyncDateTime changes - confirms device has checked in and received commands
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(10000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);
                    if (device?.LastSyncDateTime > syncBefore)
                        break;
                }
            }, ct);

        // Step 4: Wipe - wait until wipe is pending/issued, then until device is gone (only with wipe)
        if (!withWipe)
            UpdateStep("Wiper enhet", OffboardingStepStatus.Skipped, "Ikke valgt");
        else
            await RunOffboardingStepAsync("Wiper enhet", async () =>
            {
                await _intuneService.WipeManagedDeviceAsync(
                    _state.ManagedDevice.Id, _state.BuildAuditContext());

                // Poll until wipe action is registered in deviceActionResults
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);
                    var wipeAction = device?.DeviceActionResults?.FirstOrDefault(a => a.ActionName == "wipe");
                    if (wipeAction?.ActionState is "pending" or "active" or "done")
                        break;
                }

                // Poll until wipePending - confirms device has received and started wipe
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);
                    var device = await _intuneService.GetDeviceByIdAsync(_state.ManagedDevice.Id);

                    if (device is null || device.ManagementState == "wipePending")
                        break;

                    if (device.ManagementState is "wipeFailed" or "wipeCanceled")
                        throw new InvalidOperationException($"Wipe feilet med status: {device.ManagementState}.");
                }

            }, ct);

        // Step 5: Remove from Entra (only with wipe)
        if (!withWipe)
            UpdateStep("Fjerner fra Entra", OffboardingStepStatus.Skipped, "Ikke valgt");
        else if (_state.EntraDevice is null)
            UpdateStep("Fjerner fra Entra", OffboardingStepStatus.Skipped, "Ikke funnet");
        else
            await RunOffboardingStepAsync("Fjerner fra Entra", async () =>
                        await _entraDirectoryService.DeleteDeviceByAzureAdDeviceIdAsync(_state.ManagedDevice.AzureADDeviceId, 
                            _state.BuildAuditContext()), ct);

        // Step 6: Remove from Intune - skipped as device is automatically removed after wipe
        if (withWipe)
            UpdateStep("Fjerner fra Intune", ct.IsCancellationRequested
                ? OffboardingStepStatus.Skipped
                : OffboardingStepStatus.Success,
                ct.IsCancellationRequested ? "Avbrutt av bruker" : "Fjernet av wipe");
        else
            UpdateStep("Fjerner fra Intune", OffboardingStepStatus.Skipped, "Ikke valgt");

        // Step 7: Tag Defender
        if (_state.DefenderDevice is null)
            UpdateStep("Tagger i Defender", OffboardingStepStatus.Skipped, "Ikke funnet");
        else
            await RunOffboardingStepAsync("Tagger i Defender", async () =>
            {
                await _defenderService.AddMachineTagAsync(_state.DefenderDevice.Id, "Privatisert");
                await _defenderService.AddMachineTagAsync(_state.DefenderDevice.Id, "Offboardet");
            }, ct);

        // Step 8: Update status in Pureservice
        if (_state.PureserviceAssetBySn is null)
            UpdateStep("Oppdaterer status i Pureservice", OffboardingStepStatus.Skipped, "Ikke funnet");
        else
            await RunOffboardingStepAsync("Oppdaterer status i Pureservice", async () =>
                await _pureserviceService.UpdateAssetStatusAsync(
                    _state.PureserviceAssetBySn.Id.ToString(),
                    _state.PureserviceAssetBySn.TypeId), ct);

        // Step 9: Create ticket in Pureservice
        if (_state.PureserviceAssetBySn is null || _state.PureserviceUser is null)
            UpdateStep("Oppretter sak i Pureservice", OffboardingStepStatus.Skipped, "Mangler asset eller bruker i Pureservice");
        else
            await RunOffboardingStepAsync("Oppretter sak i Pureservice", async () =>
            {
                var agent = await _pureserviceService.GetUserByEmailAsync(UserCtx.Upn!)
                            ?? throw new InvalidOperationException("Kunne ikke finne Pureservice-bruker for innlogget bruker.");

                await _pureserviceService.CreateOffboardingTicketAsync(
                    subject: "Privatisering av pc",
                    description: $"<p>Maskinen skal privatiseres.</p><p>Behandlet av: {UserCtx.Upn}</p><p>LAPS-passord: {_lapsPassword ?? "ikke tilgjengelig"}</p><p>BitLocker-nøkkel: {_bitlockerKey ?? "ikke tilgjengelig"}</p>",
                    userId: _state.PureserviceUser.Id,
                    agentId: agent.Id,
                    agentDepartmentId: agent.CompanyDepartmentId,
                    assetId: _state.PureserviceAssetBySn.Id);
            }, ct);

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
            return;

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
            _offboardingSteps[index] = new OffboardingStepResult(stepName, status, message);
    }

    // Resets the offboarding state to allow starting a new offboarding process.
    private void ResetOffboarding()
    {
        _offboardingSteps = [];
        _lapsPassword = null;
        _stepSeconds = [];
        _state.ClearResults();
        StateHasChanged();
    }

    /// <summary>Returns the offboarding status title based on current step results.</summary>
    private string GetOffboardingTitle()
    {
        if (_isOffboarding)
            return "Offboarding pågår";

        if (_offboardingSteps.Any(s => s.Status == OffboardingStepStatus.Failed))
            return "Offboarding feilet";

        if (_offboardingSteps.Any(s => s.Status == OffboardingStepStatus.Skipped && s.Message == "Avbrutt av bruker"))
            return "Offboarding avbrutt";

        return "Offboarding fullført";
    }

    #endregion

}

using IntuneLight.Components.Dialogs;
using IntuneLight.Helpers;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Defender;
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
                await _entraDirectoryService.DeleteDeviceByAzureAdDeviceIdAsync(deviceId);
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
            await _intuneService.WipeManagedDeviceAsync(_state.ManagedDevice.Id);
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
            await _intuneService.RequestRemoteAssistanceAsync(_state.ManagedDevice.Id);
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
            await _intuneService.DeleteManagedDeviceAsync(_state.ManagedDevice.Id);
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
            var result = await _defenderService.RunAntiVirusScanAsync(_state.DefenderDevice.Id, scanType);

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
            await _intuneService.DeleteAutopilotDeviceAsync(_state.AutopilotDevice.Id);
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
            await _intuneService.RotateLocalAdminPasswordAsync(_state.ManagedDevice.Id);

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
                    _state.DeviceCredential = await _intuneService.GetLapsPasswordByAzureDeviceId(_state.ManagedDevice.AzureADDeviceId);
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
        Laps
    }

    #endregion
}

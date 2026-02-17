using IntuneLight.Components.Dialogs;
using IntuneLight.Models.ApiError;
using Microsoft.Identity.Client;
using MudBlazor;

namespace IntuneLight.Infrastructure;

public interface IUiErrorHandler
{
    Task HandleAsync(Exception exception);
}

public sealed class UiErrorHandler(IDialogService dialogs, ISnackbar snackbar, ILogger<UiErrorHandler> log) : IUiErrorHandler
{
    private readonly IDialogService _dialogs = dialogs;
    private readonly ISnackbar _snackbar = snackbar;
    private readonly ILogger<UiErrorHandler> _log = log;

    public async Task HandleAsync(Exception exception)
    {
        // ApiExceptions are handled in ApiResponseGuard
        if (exception is ApiException apiEx)
        {
            // Aleady logged in ApiResonseGuard
            await ShowApiErrorDialogAsync(apiEx.ErrorInfo);
            return;
        }

        // MsalServiceExceptions are handled in TokenService
        if (exception is MsalServiceException)
        {
            // Aleady logged in TokenService
            _snackbar.Add("Autentiserings-/tokenfeil. Kontakt IT.", Severity.Error, conf => conf.RequireInteraction = true);
            return;
        }

        // ArgumentExceptions are considered validation errors from UI flow
        if (exception is ArgumentException argEx)
        {
            _log.LogWarning(argEx, "Valideringsfeil i UI flow.");
            _snackbar.Add(argEx.Message, Severity.Warning, conf => conf.RequireInteraction = true);
            return;
        }

        // UiValidationExceptions are considered validation errors from UI flow
        if (exception is UiValidationException valEx)
        {
            _log.LogWarning(exception, "Validation error in UI flow. System: {System}", valEx.SystemName);
            _snackbar.Add($"{valEx.SystemName}: {valEx.Message}", Severity.Warning, conf => conf.RequireInteraction = true);
            return;
        }

        // Other unexpected errors get's logged here
        _log.LogError(exception, "Unexpected error in UI flow.");

        // Display exception message to user
        _snackbar.Add($"Uventet feil: {exception.Message}", Severity.Error, conf => conf.RequireInteraction = true);
    }

    private async Task ShowApiErrorDialogAsync(ApiErrorInfo error)
    {
        var parameters = new DialogParameters
        {
            ["Error"] = error
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = false,
            CloseButton = true,
            BackdropClick = true
        };

        await _dialogs.ShowAsync<ApiErrorDialog>("Teknisk informasjon", parameters, options);
    }


}

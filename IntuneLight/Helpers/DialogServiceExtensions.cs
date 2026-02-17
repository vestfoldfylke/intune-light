using IntuneLight.Components.Dialogs;
using MudBlazor;

namespace IntuneLight.Helpers;

public static class DialogServiceExtensions
{
    // Shows a reusable confirmation dialog and returns true only if the user confirms.

    public static async Task<bool> ConfirmIrreversibleAsync(
        this IDialogService dialogs,
        string title,
        string message,
        bool isIrreversible = true,
        string confirmText = "Slett",
        string cancelText = "Avbryt")

    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["Message"] = message,
            ["IsIrreversible"] = isIrreversible,
            ["ConfirmText"] = confirmText,
            ["CancelText"] = cancelText
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true, 
            BackdropClick = false 
        };

        var dialog = await dialogs.ShowAsync<ConfirmActionDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result != null && !result.Canceled && result.Data is bool confirmed && confirmed; // True only when confirmed
    }
}
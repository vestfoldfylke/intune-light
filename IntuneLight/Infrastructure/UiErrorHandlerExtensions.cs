namespace IntuneLight.Infrastructure;

public static class UiErrorHandlerExtensions
{
    // Executes an async action and routes exceptions to the shared UI error handler.
    public static async Task RunSafeAsync(this IUiErrorHandler handler, Func<Task> action, Action? after = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await handler.HandleAsync(ex);
        }
        finally
        {
            after?.Invoke();
        }
    }
}

namespace MiniMart.Presentation.Common;

public static class AsyncUi
{
    public static async Task<bool> RunAsync(Form owner, string operationDescription, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(action);

        var previousCursor = owner.Cursor;
        owner.Cursor = Cursors.WaitCursor;

        try
        {
            await action().ConfigureAwait(true);
            return true;
        }
        catch (Exception ex)
        {
            UiFeedback.ShowError(owner, ex, operationDescription);
            return false;
        }
        finally
        {
            if (!owner.IsDisposed)
            {
                owner.Cursor = previousCursor;
            }
        }
    }

    public static async Task<T?> RunAsync<T>(Form owner, string operationDescription, Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(action);

        var previousCursor = owner.Cursor;
        owner.Cursor = Cursors.WaitCursor;

        try
        {
            return await action().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            UiFeedback.ShowError(owner, ex, operationDescription);
            return default;
        }
        finally
        {
            if (!owner.IsDisposed)
            {
                owner.Cursor = previousCursor;
            }
        }
    }
}

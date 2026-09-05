using System.Configuration;
using Microsoft.Data.SqlClient;
using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.Presentation.Common;

public static class UiFeedback
{
    public static void ShowError(IWin32Window? owner, Exception exception, string operationDescription)
    {
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            // Expected, explainable outcome: show exactly what the rule was.
            case BusinessRuleException businessRule:
                ShowBusinessRule(owner, businessRule.Message);
                break;

            // The application has not been configured to reach a database at all.
            case ConfigurationErrorsException configuration:
                MessageBox.Show(
                    owner,
                    $"The application is not configured correctly.\n\n{configuration.Message}",
                    "Configuration problem",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                break;

            // Infrastructure failure: deliberately generic, and explicit that nothing was saved.
            case SqlException sql:
                MessageBox.Show(
                    owner,
                    $"A database problem prevented the system from being able to {operationDescription}.\n\n" +
                    "The operation did not go through, so nothing was saved. Please check the network " +
                    "and database connection, then try again.\n\n" +
                    $"Technical detail (error {sql.Number}): {sql.Message}",
                    "System error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                break;

            case OperationCanceledException:
                // The user or a closing form cancelled the work; nothing to report.
                break;

            default:
                MessageBox.Show(
                    owner,
                    $"An unexpected problem prevented the system from being able to {operationDescription}.\n\n" +
                    "The operation did not complete. Please try again, and report this to your " +
                    "administrator if it keeps happening.\n\n" +
                    $"Technical detail: {exception.Message}",
                    "Unexpected error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                break;
        }
    }

    public static void ShowBusinessRule(IWin32Window? owner, string message) =>
        MessageBox.Show(owner, message, "Cannot continue", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void ShowSuccess(IWin32Window? owner, string message, string caption = "Done") =>
        MessageBox.Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool Confirm(IWin32Window? owner, string message, string caption = "Please confirm") =>
        MessageBox.Show(owner, message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            == DialogResult.Yes;
}

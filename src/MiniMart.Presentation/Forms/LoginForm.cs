using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public partial class LoginForm : Form
{
    private readonly AppServices _services;

    public User? AuthenticatedUser { get; private set; }

    public LoginForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        InitializeComponent();
    }

    private async void LoginForm_Load(object? sender, EventArgs e)
    {
        databaseLabel.Text = $"Database: {_services.DatabaseDescription}";
        await OfferFirstRunSetupAsync().ConfigureAwait(true);
        usernameTextBox.Focus();
    }

    private async Task OfferFirstRunSetupAsync()
    {
        var anyUsers = await AsyncUi.RunAsync<bool?>(
            this,
            "check for existing user accounts",
            async () => await _services.Auth.AnyUsersExistAsync().ConfigureAwait(true)).ConfigureAwait(true);

        if (anyUsers is not false)
        {
            // Either accounts already exist, or the check failed and has been reported.
            return;
        }

        using var setup = new FirstRunSetupForm(_services);

        if (setup.ShowDialog(this) != DialogResult.OK)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        usernameTextBox.Text = setup.CreatedUsername;
        messageLabel.ForeColor = UiTheme.Success;
        messageLabel.Text = "Administrator created. Sign in with your new password.";
        passwordTextBox.Focus();
    }

    private async void SignInButton_Click(object? sender, EventArgs e)
    {
        messageLabel.Text = string.Empty;
        SetBusy(true);

        try
        {
            AuthenticatedUser = await _services.Auth.AuthenticateAsync(
                usernameTextBox.Text,
                passwordTextBox.Text).ConfigureAwait(true);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (BusinessRuleException ex)
        {
            // A rejected sign-in is an expected outcome, so it is shown inline on the form
            // rather than in a dialog the cashier has to dismiss before retyping.
            messageLabel.ForeColor = UiTheme.Danger;
            messageLabel.Text = ex.Message;
            passwordTextBox.Clear();
            passwordTextBox.Focus();
        }
        catch (Exception ex)
        {
            UiFeedback.ShowError(this, ex, "sign in");
        }
        finally
        {
            if (!IsDisposed)
            {
                SetBusy(false);
            }
        }
    }

    private void ExitButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void SetBusy(bool busy)
    {
        signInButton.Enabled = !busy;
        usernameTextBox.Enabled = !busy;
        passwordTextBox.Enabled = !busy;
        signInButton.Text = busy ? "Signing in…" : "Sign In";
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void usernameTextBox_TextChanged(object sender, EventArgs e)
    {

    }
}

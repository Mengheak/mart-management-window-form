using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class FirstRunSetupForm : Form
{
    private readonly AppServices _services;

    private TextBox _usernameTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private TextBox _confirmTextBox = null!;
    private Button _createButton = null!;

    public string CreatedUsername { get; private set; } = string.Empty;

    public FirstRunSetupForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        BuildUi();
    }

    private void BuildUi()
    {
        SuspendLayout();

        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 88 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14f),
            ForeColor = Color.White,
            Location = new Point(20, 16),
            Text = "Welcome — First-Time Setup"
        });
        headerPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Font = UiTheme.BodyFont,
            ForeColor = Color.FromArgb(190, 210, 232),
            Location = new Point(22, 48),
            Size = new Size(400, 32),
            Text = "This database has no user accounts yet. Create the first administrator to continue."
        });

        var y = 110;
        const int labelX = 24;
        const int fieldX = 160;
        const int fieldWidth = 260;

        _usernameTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = User.UsernameMaxLength,
            Size = new Size(fieldWidth, 25),
            Text = "admin"
        };
        AddRow("Username", _usernameTextBox, labelX, ref y);

        _passwordTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = 128,
            Size = new Size(fieldWidth, 25),
            UseSystemPasswordChar = true
        };
        AddRow("Password", _passwordTextBox, labelX, ref y);

        _confirmTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = 128,
            Size = new Size(fieldWidth, 25),
            UseSystemPasswordChar = true
        };
        AddRow("Confirm password", _confirmTextBox, labelX, ref y);

        Controls.Add(new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = UiTheme.MutedText,
            Location = new Point(fieldX, y),
            Size = new Size(fieldWidth, 32),
            Text = $"At least {User.MinimumPasswordLength} characters. Stored only as a salted PBKDF2 hash."
        });
        y += 40;

        _createButton = new Button
        {
            Location = new Point(fieldX + fieldWidth - 200, y),
            Size = new Size(120, 38),
            Text = "Create Admin"
        };
        UiTheme.StyleButton(_createButton, UiTheme.Success);
        _createButton.Click += async (_, _) => await CreateAsync().ConfigureAwait(true);

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(fieldX + fieldWidth - 76, y),
            Size = new Size(76, 38),
            Text = "Exit"
        };
        UiTheme.StyleButton(cancelButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));

        Controls.Add(_createButton);
        Controls.Add(cancelButton);
        Controls.Add(headerPanel);

        AcceptButton = _createButton;
        CancelButton = cancelButton;
        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(460, y + 64);
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "First-Time Setup — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private void AddRow(string caption, Control field, int labelX, ref int y)
    {
        Controls.Add(new Label { AutoSize = true, Location = new Point(labelX, y + 4), Text = caption });
        Controls.Add(field);
        y += 40;
    }

    private async Task CreateAsync()
    {
        if (_passwordTextBox.Text != _confirmTextBox.Text)
        {
            UiFeedback.ShowBusinessRule(this, "The two passwords do not match.");
            _confirmTextBox.Clear();
            _confirmTextBox.Focus();
            return;
        }

        _createButton.Enabled = false;

        var succeeded = await AsyncUi.RunAsync(
            this,
            "create the administrator account",
            () => _services.Users.CreateAsync(
                _usernameTextBox.Text,
                _passwordTextBox.Text,
                UserRole.Admin)).ConfigureAwait(true);

        if (!IsDisposed)
        {
            _createButton.Enabled = true;
        }

        if (succeeded is null)
        {
            return;
        }

        CreatedUsername = succeeded.Username;
        DialogResult = DialogResult.OK;
        Close();
    }
}

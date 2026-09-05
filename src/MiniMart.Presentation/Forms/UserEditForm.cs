using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class UserEditForm : Form
{
    private readonly AppServices _services;
    private readonly User? _existing;

    private TextBox _usernameTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private TextBox _confirmTextBox = null!;
    private ComboBox _roleCombo = null!;
    private CheckBox _activeCheckBox = null!;
    private Button _saveButton = null!;

    private bool IsNew => _existing is null;

    public UserEditForm(AppServices services, User? user)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _existing = user;

        BuildUi();
    }

    private void BuildUi()
    {
        SuspendLayout();

        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 52 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = Color.White,
            Location = new Point(18, 13),
            Text = IsNew ? "New User Account" : "Edit User Account"
        });

        var y = 74;
        const int labelX = 24;
        const int fieldX = 150;
        const int fieldWidth = 280;
        const int rowHeight = 40;

        _usernameTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = User.UsernameMaxLength,
            Size = new Size(fieldWidth, 25),
            Text = _existing?.Username ?? string.Empty
        };
        AddRow("Username", _usernameTextBox, labelX, ref y, rowHeight);

        _roleCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(fieldX, y),
            Size = new Size(fieldWidth, 25)
        };
        _roleCombo.Items.AddRange(new object[] { UserRole.Cashier, UserRole.Admin });
        _roleCombo.SelectedItem = _existing?.Role ?? UserRole.Cashier;
        AddRow("Role", _roleCombo, labelX, ref y, rowHeight);

        _passwordTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = 128,
            Size = new Size(fieldWidth, 25),
            UseSystemPasswordChar = true
        };
        AddRow(IsNew ? "Password" : "New Password", _passwordTextBox, labelX, ref y, rowHeight);

        _confirmTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = 128,
            Size = new Size(fieldWidth, 25),
            UseSystemPasswordChar = true
        };
        AddRow("Confirm", _confirmTextBox, labelX, ref y, rowHeight);

        if (!IsNew)
        {
            Controls.Add(new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.MutedText,
                Location = new Point(fieldX, y - 6),
                Size = new Size(fieldWidth, 30),
                Text = "Leave both password boxes empty to keep the current password."
            });
            y += 26;
        }

        _activeCheckBox = new CheckBox
        {
            AutoSize = true,
            Checked = _existing?.IsActive ?? true,
            Location = new Point(fieldX, y + 4),
            Text = "Account can sign in"
        };
        Controls.Add(_activeCheckBox);
        y += rowHeight;

        _saveButton = new Button
        {
            Location = new Point(fieldX + fieldWidth - 200, y + 6),
            Size = new Size(96, 36),
            Text = "Save"
        };
        UiTheme.StyleButton(_saveButton, UiTheme.Success);
        _saveButton.Click += async (_, _) => await SaveAsync().ConfigureAwait(true);

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(fieldX + fieldWidth - 96, y + 6),
            Size = new Size(96, 36),
            Text = "Cancel"
        };
        UiTheme.StyleButton(cancelButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));

        Controls.Add(_saveButton);
        Controls.Add(cancelButton);
        Controls.Add(headerPanel);

        AcceptButton = _saveButton;
        CancelButton = cancelButton;
        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(470, y + 62);
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = IsNew ? "New User" : $"Edit User — {_existing!.Username}";

        ResumeLayout(false);
        PerformLayout();
    }

    private void AddRow(string caption, Control field, int labelX, ref int y, int rowHeight)
    {
        Controls.Add(new Label { AutoSize = true, Location = new Point(labelX, y + 4), Text = caption });
        Controls.Add(field);
        y += rowHeight;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            UiFeedback.ShowBusinessRule(this, "Please enter a username.");
            _usernameTextBox.Focus();
            return;
        }

        if (_roleCombo.SelectedItem is not UserRole role)
        {
            UiFeedback.ShowBusinessRule(this, "Please choose a role.");
            return;
        }

        var password = _passwordTextBox.Text;
        var confirm = _confirmTextBox.Text;

        if (IsNew && string.IsNullOrEmpty(password))
        {
            UiFeedback.ShowBusinessRule(this, "Please set a password for the new account.");
            _passwordTextBox.Focus();
            return;
        }

        if (password != confirm)
        {
            UiFeedback.ShowBusinessRule(this, "The two passwords do not match.");
            _confirmTextBox.Clear();
            _confirmTextBox.Focus();
            return;
        }

        _saveButton.Enabled = false;

        var succeeded = await AsyncUi.RunAsync(this, "save the user account", async () =>
        {
            if (IsNew)
            {
                await _services.Users
                    .CreateAsync(_usernameTextBox.Text, password, role)
                    .ConfigureAwait(true);
            }
            else
            {
                await _services.Users.UpdateAsync(
                    _existing!.UserId,
                    _usernameTextBox.Text,
                    role,
                    _activeCheckBox.Checked,
                    string.IsNullOrEmpty(password) ? null : password).ConfigureAwait(true);
            }
        }).ConfigureAwait(true);

        if (!IsDisposed)
        {
            _saveButton.Enabled = true;
        }

        if (succeeded)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

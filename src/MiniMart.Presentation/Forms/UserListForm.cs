using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class UserListForm : Form
{
    private readonly AppServices _services;
    private readonly User _currentAdmin;

    private DataGridView _grid = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public UserListForm(AppServices services, User currentAdmin)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _currentAdmin = currentAdmin ?? throw new ArgumentNullException(nameof(currentAdmin));

        BuildUi();
        Load += async (_, _) => await LoadUsersAsync().ConfigureAwait(true);
    }

    private void BuildUi()
    {
        SuspendLayout();

        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 56 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14f),
            ForeColor = Color.White,
            Location = new Point(18, 14),
            Text = "User Management"
        });

        _grid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_grid);
        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(User.UserId),
                HeaderText = "ID",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable
            },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(User.Username),
                HeaderText = "Username",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(User.Role),
                HeaderText = "Role",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable
            },
            new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(User.IsActive),
                HeaderText = "Active",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(User.CreatedAt),
                HeaderText = "Created",
                Width = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" },
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        _grid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditSelectedAsync().ConfigureAwait(true);
            }
        };

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };
        gridHost.Controls.Add(_grid);

        var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(16, 10, 16, 12) };

        var newButton = new Button { Location = new Point(16, 10), Size = new Size(110, 36), Text = "New User" };
        UiTheme.StyleButton(newButton, UiTheme.Success);
        newButton.Click += async (_, _) => await CreateAsync().ConfigureAwait(true);

        var editButton = new Button { Location = new Point(136, 10), Size = new Size(100, 36), Text = "Edit" };
        UiTheme.StyleButton(editButton, UiTheme.Primary);
        editButton.Click += async (_, _) => await EditSelectedAsync().ConfigureAwait(true);

        var deleteButton = new Button { Location = new Point(246, 10), Size = new Size(100, 36), Text = "Delete" };
        UiTheme.StyleButton(deleteButton, UiTheme.Danger);
        deleteButton.Click += async (_, _) => await DeleteSelectedAsync().ConfigureAwait(true);

        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(660, 10),
            Size = new Size(100, 36),
            Text = "Close"
        };
        UiTheme.StyleButton(closeButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));
        closeButton.Click += (_, _) => Close();

        actionPanel.Controls.Add(closeButton);
        actionPanel.Controls.Add(deleteButton);
        actionPanel.Controls.Add(editButton);
        actionPanel.Controls.Add(newButton);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(800, 540);
        Controls.Add(gridHost);
        Controls.Add(actionPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(700, 440);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Users — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private async Task LoadUsersAsync()
    {
        var users = await AsyncUi.RunAsync(
            this,
            "load the user list",
            () => _services.Users.GetAllAsync()).ConfigureAwait(true);

        if (users is null)
        {
            return;
        }

        _grid.DataSource = users.ToList();
        _statusLabel.Text =
            $"{users.Count} account(s) — {users.Count(u => u.IsAdmin && u.IsActive)} active administrator(s)";
    }

    private User? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is User user)
        {
            return user;
        }

        UiFeedback.ShowBusinessRule(this, "Select a user from the list first.");
        return null;
    }

    private async Task CreateAsync()
    {
        using var editor = new UserEditForm(_services, user: null);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            await LoadUsersAsync().ConfigureAwait(true);
        }
    }

    private async Task EditSelectedAsync()
    {
        if (GetSelected() is not { } user)
        {
            return;
        }

        using var editor = new UserEditForm(_services, user);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            await LoadUsersAsync().ConfigureAwait(true);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (GetSelected() is not { } user)
        {
            return;
        }

        if (user.UserId == _currentAdmin.UserId)
        {
            UiFeedback.ShowBusinessRule(this, "You cannot delete the account you are currently signed in with.");
            return;
        }

        if (!UiFeedback.Confirm(this, $"Delete the account '{user.Username}'?", "Delete user"))
        {
            return;
        }

        // bool? rather than bool so a failure (null) stays distinguishable from a successful
        // deactivation (false).
        var deleted = await AsyncUi.RunAsync<bool?>(
            this,
            "delete the user account",
            async () => await _services.Users
                .DeleteAsync(user.UserId)
                .ConfigureAwait(true)).ConfigureAwait(true);

        if (deleted is null)
        {
            return;
        }

        UiFeedback.ShowSuccess(
            this,
            deleted.Value
                ? $"The account '{user.Username}' was deleted."
                : $"'{user.Username}' has processed sales, so the account was deactivated instead of deleted. " +
                  "It can no longer sign in, and past sales still show who processed them.",
            "User removed");

        await LoadUsersAsync().ConfigureAwait(true);
    }
}

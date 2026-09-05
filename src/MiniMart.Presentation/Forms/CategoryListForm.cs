using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class CategoryListForm : Form
{
    private readonly AppServices _services;

    private DataGridView _grid = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public CategoryListForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        BuildUi();
        Load += async (_, _) => await LoadCategoriesAsync().ConfigureAwait(true);
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
            Text = "Category Management"
        });

        _grid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_grid);
        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Category.CategoryId),
                HeaderText = "ID",
                Width = 70,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable
            },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Category.Name),
                HeaderText = "Category Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        _grid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await RenameSelectedAsync().ConfigureAwait(true);
            }
        };

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };
        gridHost.Controls.Add(_grid);

        var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(16, 10, 16, 12) };

        var newButton = new Button { Location = new Point(16, 10), Size = new Size(126, 36), Text = "New Category" };
        UiTheme.StyleButton(newButton, UiTheme.Success);
        newButton.Click += async (_, _) => await CreateAsync().ConfigureAwait(true);

        var renameButton = new Button { Location = new Point(152, 10), Size = new Size(100, 36), Text = "Rename" };
        UiTheme.StyleButton(renameButton, UiTheme.Primary);
        renameButton.Click += async (_, _) => await RenameSelectedAsync().ConfigureAwait(true);

        var deleteButton = new Button { Location = new Point(262, 10), Size = new Size(100, 36), Text = "Delete" };
        UiTheme.StyleButton(deleteButton, UiTheme.Danger);
        deleteButton.Click += async (_, _) => await DeleteSelectedAsync().ConfigureAwait(true);

        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(500, 10),
            Size = new Size(100, 36),
            Text = "Close"
        };
        UiTheme.StyleButton(closeButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));
        closeButton.Click += (_, _) => Close();

        actionPanel.Controls.Add(closeButton);
        actionPanel.Controls.Add(deleteButton);
        actionPanel.Controls.Add(renameButton);
        actionPanel.Controls.Add(newButton);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(640, 520);
        Controls.Add(gridHost);
        Controls.Add(actionPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(520, 420);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Categories — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await AsyncUi.RunAsync(
            this,
            "load the category list",
            () => _services.Categories.GetAllAsync()).ConfigureAwait(true);

        if (categories is null)
        {
            return;
        }

        _grid.DataSource = categories.ToList();
        _statusLabel.Text = $"{categories.Count} category/categories";
    }

    private Category? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Category category)
        {
            return category;
        }

        UiFeedback.ShowBusinessRule(this, "Select a category from the list first.");
        return null;
    }

    private async Task CreateAsync()
    {
        using var prompt = new TextPromptForm("New Category", "Category name", string.Empty, Category.NameMaxLength);

        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var succeeded = await AsyncUi.RunAsync(
            this,
            "create the category",
            () => _services.Categories.CreateAsync(prompt.Value)).ConfigureAwait(true);

        if (succeeded is not null)
        {
            await LoadCategoriesAsync().ConfigureAwait(true);
        }
    }

    private async Task RenameSelectedAsync()
    {
        if (GetSelected() is not { } category)
        {
            return;
        }

        using var prompt = new TextPromptForm(
            "Rename Category",
            "Category name",
            category.Name,
            Category.NameMaxLength);

        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var succeeded = await AsyncUi.RunAsync(
            this,
            "rename the category",
            () => _services.Categories.UpdateAsync(category.CategoryId, prompt.Value)).ConfigureAwait(true);

        if (succeeded)
        {
            await LoadCategoriesAsync().ConfigureAwait(true);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (GetSelected() is not { } category)
        {
            return;
        }

        if (!UiFeedback.Confirm(this, $"Delete the category '{category.Name}'?", "Delete category"))
        {
            return;
        }

        var succeeded = await AsyncUi.RunAsync(
            this,
            "delete the category",
            () => _services.Categories.DeleteAsync(category.CategoryId)).ConfigureAwait(true);

        if (succeeded)
        {
            await LoadCategoriesAsync().ConfigureAwait(true);
        }
    }
}

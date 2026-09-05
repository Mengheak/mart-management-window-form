using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class ProductListForm : Form
{
    private readonly AppServices _services;

    private TextBox _searchTextBox = null!;
    private CheckBox _includeInactiveCheckBox = null!;
    private DataGridView _grid = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public ProductListForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        BuildUi();
        Load += async (_, _) => await LoadProductsAsync().ConfigureAwait(true);
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
            Text = "Product Management"
        });

        // ── Filter bar ───────────────────────────────────────────────────────
        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 12, 16, 8) };

        _searchTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 11f),
            Location = new Point(16, 12),
            PlaceholderText = "Search by name or code…",
            Size = new Size(320, 27)
        };
        _searchTextBox.TextChanged += async (_, _) => await LoadProductsAsync().ConfigureAwait(true);

        _includeInactiveCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(352, 16),
            Text = "Include discontinued"
        };
        _includeInactiveCheckBox.CheckedChanged += async (_, _) => await LoadProductsAsync().ConfigureAwait(true);

        filterPanel.Controls.Add(_includeInactiveCheckBox);
        filterPanel.Controls.Add(_searchTextBox);

        // ── Grid ─────────────────────────────────────────────────────────────
        _grid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_grid);
        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            TextColumn("Sku", "Code", 100),
            TextColumn("Name", "Product Name", 0),
            TextColumn("CategoryName", "Category", 140),
            MoneyColumn("UnitPrice", "Unit Price"),
            MoneyColumn("CostPrice", "Cost Price"),
            NumberColumn("StockQuantity", "Stock"),
            NumberColumn("ReorderLevel", "Reorder At"),
            new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsActive",
                HeaderText = "Active",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });
        _grid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditSelectedAsync().ConfigureAwait(true);
            }
        };
        // Highlights rows that have fallen to or below their reorder level.
        _grid.RowPrePaint += (_, args) =>
        {
            if (_grid.Rows[args.RowIndex].DataBoundItem is Product { IsLowStock: true })
            {
                _grid.Rows[args.RowIndex].DefaultCellStyle.BackColor = UiTheme.Warning;
            }
        };

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 8) };
        gridHost.Controls.Add(_grid);

        // ── Actions ──────────────────────────────────────────────────────────
        var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(16, 10, 16, 12) };

        var newButton = MakeActionButton("New Product", 16, UiTheme.Success);
        newButton.Click += async (_, _) => await CreateAsync().ConfigureAwait(true);

        var editButton = MakeActionButton("Edit", 156, UiTheme.Primary);
        editButton.Click += async (_, _) => await EditSelectedAsync().ConfigureAwait(true);

        var stockButton = MakeActionButton("Adjust Stock", 266, UiTheme.PrimaryLight);
        stockButton.Click += async (_, _) => await AdjustStockAsync().ConfigureAwait(true);

        var deleteButton = MakeActionButton("Delete", 396, UiTheme.Danger);
        deleteButton.Click += async (_, _) => await DeleteSelectedAsync().ConfigureAwait(true);

        var closeButton = MakeActionButton("Close", 0, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));
        closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        closeButton.Location = new Point(ClientSize.Width - 130, 10);
        closeButton.Click += (_, _) => Close();

        actionPanel.Controls.Add(closeButton);
        actionPanel.Controls.Add(deleteButton);
        actionPanel.Controls.Add(stockButton);
        actionPanel.Controls.Add(editButton);
        actionPanel.Controls.Add(newButton);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(1080, 640);
        Controls.Add(gridHost);
        Controls.Add(actionPanel);
        Controls.Add(filterPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(900, 520);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Products — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private static Button MakeActionButton(string text, int x, Color back, Color? fore = null)
    {
        var button = new Button { Location = new Point(x, 10), Size = new Size(126, 36), Text = text };
        UiTheme.StyleButton(button, back, fore);
        return button;
    }

    private static DataGridViewTextBoxColumn TextColumn(string property, string header, int width) =>
        new()
        {
            DataPropertyName = property,
            HeaderText = header,
            Width = width == 0 ? 100 : width,
            AutoSizeMode = width == 0
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

    private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
    {
        var column = TextColumn(property, header, 96);
        column.DefaultCellStyle.Format = "N2";
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private static DataGridViewTextBoxColumn NumberColumn(string property, string header)
    {
        var column = TextColumn(property, header, 82);
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private async Task LoadProductsAsync()
    {
        var products = await AsyncUi.RunAsync(this, "load the product list", async () =>
        {
            var all = await _services.Inventory
                .GetProductsAsync(_includeInactiveCheckBox.Checked)
                .ConfigureAwait(true);

            var term = _searchTextBox.Text.Trim();
            if (term.Length == 0)
            {
                return all;
            }

            return (IReadOnlyList<Product>)all
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }).ConfigureAwait(true);

        if (products is null)
        {
            return;
        }

        _grid.DataSource = products.ToList();
        _statusLabel.Text = $"{products.Count} product(s) — {products.Count(p => p.IsLowStock)} low on stock";
    }

    private Product? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Product product)
        {
            return product;
        }

        UiFeedback.ShowBusinessRule(this, "Select a product from the list first.");
        return null;
    }

    private async Task CreateAsync()
    {
        using var editor = new ProductEditForm(_services, product: null);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            await LoadProductsAsync().ConfigureAwait(true);
        }
    }

    private async Task EditSelectedAsync()
    {
        if (GetSelected() is not { } product)
        {
            return;
        }

        using var editor = new ProductEditForm(_services, product);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            await LoadProductsAsync().ConfigureAwait(true);
        }
    }

    private async Task AdjustStockAsync()
    {
        if (GetSelected() is not { } product)
        {
            return;
        }

        using var prompt = new StockAdjustmentForm(product);

        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var succeeded = await AsyncUi.RunAsync(
            this,
            "adjust the stock level",
            () => _services.Inventory.AdjustStockAsync(product.ProductId, prompt.NewQuantity)).ConfigureAwait(true);

        if (succeeded)
        {
            _statusLabel.Text = $"Stock for {product.Name} set to {prompt.NewQuantity}";
            await LoadProductsAsync().ConfigureAwait(true);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (GetSelected() is not { } product)
        {
            return;
        }

        if (!UiFeedback.Confirm(this, $"Remove '{product.Name}' from the catalogue?", "Delete product"))
        {
            return;
        }

        // bool? rather than bool so a failure (null) stays distinguishable from a successful
        // deactivation (false).
        var deleted = await AsyncUi.RunAsync<bool?>(
            this,
            "delete the product",
            async () => await _services.Inventory
                .DeleteProductAsync(product.ProductId)
                .ConfigureAwait(true)).ConfigureAwait(true);

        if (deleted is null)
        {
            return;
        }

        // A product with sales history is deactivated rather than deleted, so historical
        // receipts keep resolving. Say so plainly rather than reporting a plain "deleted".
        UiFeedback.ShowSuccess(
            this,
            deleted.Value
                ? $"'{product.Name}' was deleted."
                : $"'{product.Name}' appears in past sales, so it was marked discontinued instead of deleted. " +
                  "It will no longer appear at the till.",
            "Product removed");

        await LoadProductsAsync().ConfigureAwait(true);
    }
}

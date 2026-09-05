using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class LowStockForm : Form
{
    private readonly AppServices _services;

    private DataGridView _grid = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public LowStockForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        BuildUi();
        Load += async (_, _) => await LoadAsync().ConfigureAwait(true);
    }

    private void BuildUi()
    {
        SuspendLayout();

        var headerPanel = new Panel { BackColor = UiTheme.Danger, Dock = DockStyle.Top, Height = 66 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14f),
            ForeColor = Color.White,
            Location = new Point(18, 12),
            Text = "Low Stock — Reorder Required"
        });
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = UiTheme.BodyFont,
            ForeColor = Color.FromArgb(250, 220, 220),
            Location = new Point(20, 40),
            Text = "Active products at or below their reorder level, most depleted first."
        });

        _grid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_grid);
        _grid.AutoGenerateColumns = false;
        _grid.Columns.AddRange(
            Column(nameof(Product.Sku), "Code", 110),
            Column(nameof(Product.Name), "Product", 0),
            Column(nameof(Product.CategoryName), "Category", 150),
            NumberColumn(nameof(Product.StockQuantity), "On Hand"),
            NumberColumn(nameof(Product.ReorderLevel), "Reorder At"),
            MoneyColumn(nameof(Product.UnitPrice), "Unit Price"));

        // Out-of-stock products are the urgent ones, so they get the stronger highlight.
        _grid.RowPrePaint += (_, args) =>
        {
            if (_grid.Rows[args.RowIndex].DataBoundItem is Product product)
            {
                _grid.Rows[args.RowIndex].DefaultCellStyle.BackColor = product.StockQuantity == 0
                    ? Color.FromArgb(255, 226, 226)
                    : UiTheme.Warning;
            }
        };

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };
        gridHost.Controls.Add(_grid);

        var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(16, 10, 16, 12) };

        var refreshButton = new Button { Location = new Point(16, 10), Size = new Size(100, 36), Text = "Refresh" };
        UiTheme.StyleButton(refreshButton, UiTheme.Primary);
        refreshButton.Click += async (_, _) => await LoadAsync().ConfigureAwait(true);

        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(740, 10),
            Size = new Size(100, 36),
            Text = "Close"
        };
        UiTheme.StyleButton(closeButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));
        closeButton.Click += (_, _) => Close();

        actionPanel.Controls.Add(closeButton);
        actionPanel.Controls.Add(refreshButton);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(880, 560);
        Controls.Add(gridHost);
        Controls.Add(actionPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(760, 460);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Low Stock — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private static DataGridViewTextBoxColumn Column(string property, string header, int width) =>
        new()
        {
            DataPropertyName = property,
            HeaderText = header,
            Width = width == 0 ? 120 : width,
            AutoSizeMode = width == 0
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

    private static DataGridViewTextBoxColumn NumberColumn(string property, string header)
    {
        var column = Column(property, header, 100);
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
    {
        var column = Column(property, header, 110);
        column.DefaultCellStyle.Format = "N2";
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private async Task LoadAsync()
    {
        var products = await AsyncUi.RunAsync(
            this,
            "load the low-stock list",
            () => _services.Inventory.GetLowStockProductsAsync()).ConfigureAwait(true);

        if (products is null)
        {
            return;
        }

        _grid.DataSource = products.ToList();
        var outOfStock = products.Count(p => p.StockQuantity == 0);
        _statusLabel.Text = products.Count == 0
            ? "All products are above their reorder level."
            : $"{products.Count} product(s) need reordering — {outOfStock} completely out of stock";
    }
}

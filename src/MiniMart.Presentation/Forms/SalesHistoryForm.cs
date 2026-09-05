using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class SalesHistoryForm : Form
{
    private readonly AppServices _services;

    private DateTimePicker _fromPicker = null!;
    private DateTimePicker _toPicker = null!;
    private DataGridView _salesGrid = null!;
    private DataGridView _linesGrid = null!;
    private DataGridView _dailyGrid = null!;
    private DataGridView _bestSellerGrid = null!;
    private Label _summaryLabel = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public SalesHistoryForm(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        BuildUi();
        Load += async (_, _) => await RefreshAllAsync().ConfigureAwait(true);
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
            Text = "Sales History & Reports"
        });

        // ── Date range filter ────────────────────────────────────────────────
        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(16, 12, 16, 8) };

        _fromPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(60, 14),
            Size = new Size(130, 25),
            Value = DateTime.Today.AddDays(-30)
        };

        _toPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(228, 14),
            Size = new Size(130, 25),
            Value = DateTime.Today
        };

        var applyButton = new Button { Location = new Point(374, 12), Size = new Size(90, 30), Text = "Apply" };
        UiTheme.StyleButton(applyButton, UiTheme.Primary);
        applyButton.Click += async (_, _) => await RefreshAllAsync().ConfigureAwait(true);

        var todayButton = new Button { Location = new Point(472, 12), Size = new Size(90, 30), Text = "Today" };
        UiTheme.StyleButton(todayButton, UiTheme.PrimaryLight);
        todayButton.Click += async (_, _) =>
        {
            _fromPicker.Value = DateTime.Today;
            _toPicker.Value = DateTime.Today;
            await RefreshAllAsync().ConfigureAwait(true);
        };

        _summaryLabel = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = UiTheme.Primary,
            Location = new Point(600, 16),
            Size = new Size(340, 26),
            TextAlign = ContentAlignment.MiddleRight
        };

        filterPanel.Controls.Add(_summaryLabel);
        filterPanel.Controls.Add(todayButton);
        filterPanel.Controls.Add(applyButton);
        filterPanel.Controls.Add(new Label { AutoSize = true, Location = new Point(16, 18), Text = "From" });
        filterPanel.Controls.Add(_fromPicker);
        filterPanel.Controls.Add(new Label { AutoSize = true, Location = new Point(200, 18), Text = "To" });
        filterPanel.Controls.Add(_toPicker);

        // ── Tabs ─────────────────────────────────────────────────────────────
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };

        tabs.TabPages.Add(BuildTransactionsTab());
        tabs.TabPages.Add(BuildDailyTotalsTab());
        tabs.TabPages.Add(BuildBestSellersTab());

        var tabHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 4, 16, 8) };
        tabHost.Controls.Add(tabs);

        var closePanel = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(16, 10, 16, 10) };
        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(880, 8),
            Size = new Size(100, 36),
            Text = "Close"
        };
        UiTheme.StyleButton(closeButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));
        closeButton.Click += (_, _) => Close();
        closePanel.Controls.Add(closeButton);

        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(1020, 700);
        Controls.Add(tabHost);
        Controls.Add(closePanel);
        Controls.Add(filterPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Sales History — Mart Management System";

        ResumeLayout(false);
        PerformLayout();
    }

    private TabPage BuildTransactionsTab()
    {
        var page = new TabPage("Transactions") { BackColor = UiTheme.Surface, Padding = new Padding(8) };

        _salesGrid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_salesGrid);
        _salesGrid.AutoGenerateColumns = false;
        _salesGrid.Columns.AddRange(
            Column(nameof(Sale.SaleId), "Sale #", 70),
            DateColumn(nameof(Sale.SaleDate), "Date & Time", 150),
            Column(nameof(Sale.CashierName), "Cashier", 0),
            MoneyColumn(nameof(Sale.TotalAmount), "Total"),
            MoneyColumn(nameof(Sale.AmountPaid), "Paid"),
            MoneyColumn(nameof(Sale.ChangeDue), "Change"));
        _salesGrid.SelectionChanged += async (_, _) => await LoadSelectedSaleLinesAsync().ConfigureAwait(true);

        _linesGrid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_linesGrid);
        _linesGrid.AutoGenerateColumns = false;
        _linesGrid.Columns.AddRange(
            Column(nameof(SaleDetail.ProductName), "Product", 0),
            Column(nameof(SaleDetail.Quantity), "Qty", 70),
            MoneyColumn(nameof(SaleDetail.UnitPrice), "Unit Price"),
            MoneyColumn(nameof(SaleDetail.LineTotal), "Line Total"));

        var linesPanel = new Panel { Dock = DockStyle.Bottom, Height = 220 };
        linesPanel.Controls.Add(_linesGrid);
        linesPanel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI Semibold", 10f),
            Height = 28,
            Text = "Line items for the selected sale",
            TextAlign = ContentAlignment.MiddleLeft
        });

        page.Controls.Add(_salesGrid);
        page.Controls.Add(linesPanel);
        return page;
    }

    private TabPage BuildDailyTotalsTab()
    {
        var page = new TabPage("Daily Totals") { BackColor = UiTheme.Surface, Padding = new Padding(8) };

        _dailyGrid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_dailyGrid);
        _dailyGrid.AutoGenerateColumns = false;
        _dailyGrid.Columns.AddRange(
            DateOnlyColumn("Date", "Date", 140),
            Column("SaleCount", "Sales", 100),
            Column("UnitsSold", "Units Sold", 110),
            MoneyColumn("TotalRevenue", "Revenue"),
            MoneyColumn("AverageSaleValue", "Average Sale"));

        page.Controls.Add(_dailyGrid);
        return page;
    }

    private TabPage BuildBestSellersTab()
    {
        var page = new TabPage("Best Sellers") { BackColor = UiTheme.Surface, Padding = new Padding(8) };

        _bestSellerGrid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_bestSellerGrid);
        _bestSellerGrid.AutoGenerateColumns = false;
        _bestSellerGrid.Columns.AddRange(
            Column("Sku", "Code", 110),
            Column("ProductName", "Product", 0),
            Column("UnitsSold", "Units Sold", 110),
            MoneyColumn("Revenue", "Revenue"));

        page.Controls.Add(_bestSellerGrid);
        return page;
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

    private static DataGridViewTextBoxColumn DateColumn(string property, string header, int width)
    {
        var column = Column(property, header, width);
        column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        return column;
    }

    private static DataGridViewTextBoxColumn DateOnlyColumn(string property, string header, int width)
    {
        var column = Column(property, header, width);
        column.DefaultCellStyle.Format = "yyyy-MM-dd";
        return column;
    }

    private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
    {
        var column = Column(property, header, 110);
        column.DefaultCellStyle.Format = "N2";
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private async Task RefreshAllAsync()
    {
        await AsyncUi.RunAsync(this, "load the sales reports", async () =>
        {
            var from = _fromPicker.Value.Date;
            var to = _toPicker.Value.Date;

            var sales = await _services.Reporting.GetSalesAsync(from, to).ConfigureAwait(true);
            _salesGrid.DataSource = sales.ToList();

            var daily = await _services.Reporting.GetDailyTotalsAsync(from, to).ConfigureAwait(true);
            _dailyGrid.DataSource = daily.ToList();

            var bestSellers = await _services.Reporting
                .GetBestSellingProductsAsync(from, to, 20)
                .ConfigureAwait(true);
            _bestSellerGrid.DataSource = bestSellers.ToList();

            var revenue = sales.Sum(s => s.TotalAmount);
            _summaryLabel.Text = $"{sales.Count} sale(s) · {UiTheme.Money(revenue)}";
            _statusLabel.Text = $"Range {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";

            if (sales.Count == 0)
            {
                _linesGrid.DataSource = null;
            }
        }).ConfigureAwait(true);
    }

    private async Task LoadSelectedSaleLinesAsync()
    {
        if (_salesGrid.CurrentRow?.DataBoundItem is not Sale sale)
        {
            _linesGrid.DataSource = null;
            return;
        }

        var lines = await AsyncUi.RunAsync(
            this,
            "load the sale's line items",
            () => _services.Reporting.GetSaleLinesAsync(sale.SaleId)).ConfigureAwait(true);

        if (lines is not null)
        {
            _linesGrid.DataSource = lines.ToList();
        }
    }
}

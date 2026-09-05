using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class AdminDashboardForm : Form, ILogoutAware
{
    private readonly AppServices _services;
    private readonly User _admin;

    private Label _todaySalesValue = null!;
    private Label _todayRevenueValue = null!;
    private Label _lowStockValue = null!;
    private Label _productCountValue = null!;
    private DataGridView _recentSalesGrid = null!;
    private Label _userLabel = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    public bool LogOutRequested { get; private set; }

    public AdminDashboardForm(AppServices services, User admin)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _admin = admin ?? throw new ArgumentNullException(nameof(admin));

        BuildUi();
        Load += async (_, _) => await RefreshDashboardAsync().ConfigureAwait(true);
    }

    private void BuildUi()
    {
        SuspendLayout();

        // ── Header ───────────────────────────────────────────────────────────
        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 64 };

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15f),
            ForeColor = Color.White,
            Location = new Point(20, 17),
            Text = "Mart Management System — Administration"
        };

        _userLabel = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            Font = UiTheme.BodyFont,
            ForeColor = Color.FromArgb(190, 210, 232),
            Location = new Point(640, 22),
            Size = new Size(340, 22),
            Text = $"Signed in as {_admin.Username} (Admin)",
            TextAlign = ContentAlignment.MiddleRight
        };

        var logoutButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(990, 16),
            Size = new Size(110, 32),
            Text = "Sign Out"
        };
        UiTheme.StyleButton(logoutButton, UiTheme.PrimaryLight);
        logoutButton.Click += (_, _) =>
        {
            LogOutRequested = true;
            Close();
        };

        headerPanel.Controls.Add(logoutButton);
        headerPanel.Controls.Add(_userLabel);
        headerPanel.Controls.Add(titleLabel);

        // ── Navigation ───────────────────────────────────────────────────────
        var navPanel = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Left,
            Padding = new Padding(16, 20, 16, 20),
            Width = 236
        };

        // Added in reverse order because each Dock=Top control stacks above the previous one.
        AddNavButton(navPanel, "Refresh Dashboard", async () => await RefreshDashboardAsync().ConfigureAwait(true));
        AddNavButton(navPanel, "Point of Sale", OpenPos);
        AddNavButton(navPanel, "Low Stock", () => ShowChild(new LowStockForm(_services)));
        AddNavButton(navPanel, "Sales History && Reports", () => ShowChild(new SalesHistoryForm(_services)));
        AddNavButton(navPanel, "Users", () => ShowChild(new UserListForm(_services, _admin)));
        AddNavButton(navPanel, "Categories", () => ShowChild(new CategoryListForm(_services)));
        AddNavButton(navPanel, "Products", () => ShowChild(new ProductListForm(_services)));

        var navHeading = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = UiTheme.MutedText,
            Height = 34,
            Text = "MANAGE",
            TextAlign = ContentAlignment.MiddleLeft
        };
        navPanel.Controls.Add(navHeading);

        // ── Content ──────────────────────────────────────────────────────────
        var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 20) };

        var tilePanel = new TableLayoutPanel
        {
            ColumnCount = 4,
            Dock = DockStyle.Top,
            Height = 130,
            RowCount = 1
        };
        for (var i = 0; i < 4; i++)
        {
            tilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        tilePanel.Controls.Add(BuildTile("Sales Today", out _todaySalesValue, UiTheme.Primary), 0, 0);
        tilePanel.Controls.Add(BuildTile("Revenue Today", out _todayRevenueValue, UiTheme.Success), 1, 0);
        tilePanel.Controls.Add(BuildTile("Low Stock Items", out _lowStockValue, UiTheme.Danger), 2, 0);
        tilePanel.Controls.Add(BuildTile("Active Products", out _productCountValue, UiTheme.PrimaryLight), 3, 0);

        var recentHeading = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI Semibold", 12f),
            Height = 40,
            Padding = new Padding(0, 12, 0, 0),
            Text = "Recent Sales"
        };

        _recentSalesGrid = new DataGridView { Dock = DockStyle.Fill };
        UiTheme.StyleGrid(_recentSalesGrid);
        _recentSalesGrid.AutoGenerateColumns = false;
        _recentSalesGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SaleId",
                HeaderText = "Sale #",
                Width = 80,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SaleDate",
                HeaderText = "Date & Time",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            },
            new DataGridViewTextBoxColumn { DataPropertyName = "CashierName", HeaderText = "Cashier" },
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalAmount",
                HeaderText = "Total",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

        contentPanel.Controls.Add(_recentSalesGrid);
        contentPanel.Controls.Add(recentHeading);
        contentPanel.Controls.Add(tilePanel);

        // ── Status bar ───────────────────────────────────────────────────────
        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel { Text = "Ready" };
        statusStrip.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(1140, 720);
        Controls.Add(contentPanel);
        Controls.Add(navPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(1040, 660);
        Name = "AdminDashboardForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Administration — Mart Management System";
        WindowState = FormWindowState.Maximized;

        ResumeLayout(false);
        PerformLayout();
    }

    private static void AddNavButton(Control parent, string text, Action onClick)
    {
        var button = new Button
        {
            Dock = DockStyle.Top,
            Height = 44,
            Margin = new Padding(0, 0, 0, 8),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };
        UiTheme.StyleButton(button, Color.FromArgb(238, 242, 247), Color.FromArgb(40, 55, 75));
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 230, 242);
        button.Click += (_, _) => onClick();

        parent.Controls.Add(button);
        button.BringToFront();
    }

    private static Panel BuildTile(string caption, out Label valueLabel, Color accent)
    {
        var tile = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(16, 14, 16, 14)
        };

        valueLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = accent,
            Text = "—",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var captionLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.MutedText,
            Height = 24,
            Text = caption
        };

        tile.Controls.Add(valueLabel);
        tile.Controls.Add(captionLabel);
        return tile;
    }

    private async Task RefreshDashboardAsync()
    {
        await AsyncUi.RunAsync(this, "load the dashboard", async () =>
        {
            var today = DateTime.Today;

            var todayTotals = await _services.Reporting
                .GetDailyTotalsAsync(today, today)
                .ConfigureAwait(true);

            var todayRow = todayTotals.FirstOrDefault();
            _todaySalesValue.Text = (todayRow?.SaleCount ?? 0).ToString();
            _todayRevenueValue.Text = UiTheme.Money(todayRow?.TotalRevenue ?? 0m);

            var lowStock = await _services.Inventory.GetLowStockProductsAsync().ConfigureAwait(true);
            _lowStockValue.Text = lowStock.Count.ToString();

            var products = await _services.Inventory.GetProductsAsync().ConfigureAwait(true);
            _productCountValue.Text = products.Count.ToString();

            var recentSales = await _services.Reporting
                .GetSalesAsync(today.AddDays(-30), today)
                .ConfigureAwait(true);

            _recentSalesGrid.DataSource = recentSales.Take(15).ToList();
            _statusLabel.Text = $"Updated {DateTime.Now:HH:mm:ss} — database {_services.DatabaseDescription}";
        }).ConfigureAwait(true);
    }

    private async void ShowChild(Form child)
    {
        using (child)
        {
            child.ShowDialog(this);
        }

        await RefreshDashboardAsync().ConfigureAwait(true);
    }

    private async void OpenPos()
    {
        using (var pos = new PosForm(_services, _admin))
        {
            pos.ShowDialog(this);
        }

        await RefreshDashboardAsync().ConfigureAwait(true);
    }
}

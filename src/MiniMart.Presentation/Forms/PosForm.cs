using MiniMart.BusinessLogic.Discounts;
using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Services;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public partial class PosForm : Form, ILogoutAware
{
    private readonly AppServices _services;
    private readonly User _cashier;
    private readonly SaleService _saleService;
    private readonly Cart _cart = new();
    private TextBox _khrPaidTextBox = null!;
    private NumericUpDown _rateNumeric = null!;
    private Label _khrTotalValueLabel = null!;
    private Label _khrBalanceCaptionLabel = null!;
    private Label _khrBalanceValueLabel = null!;
    private int _productSearchVersion;

    public bool LogOutRequested { get; private set; }

    public PosForm(AppServices services, User cashier)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _cashier = cashier ?? throw new ArgumentNullException(nameof(cashier));
        _saleService = services.CreateSaleService();

        InitializeComponent();
        ConfigurePosDisplay();
    }

    private void ConfigurePosDisplay()
    {
        UiTheme.StyleGrid(productGrid);
        productGrid.AutoGenerateColumns = false;
        productGrid.Columns.AddRange(
            MakeColumn(nameof(Product.Sku), "Code", 110, DataGridViewAutoSizeColumnMode.None),
            MakeColumn(nameof(Product.Name), "Product", 180, DataGridViewAutoSizeColumnMode.Fill),
            MakeMoneyColumn(nameof(Product.UnitPrice), "Price", 110),
            MakeColumn(nameof(Product.StockQuantity), "Stock", 70, DataGridViewAutoSizeColumnMode.None));

        UiTheme.StyleGrid(cartGrid);
        cartGrid.AutoGenerateColumns = false;
        cartGrid.Columns.AddRange(
            MakeColumn(nameof(CartItem.ProductName), "Product", 180, DataGridViewAutoSizeColumnMode.Fill),
            MakeColumn(nameof(CartItem.Quantity), "Qty", 60, DataGridViewAutoSizeColumnMode.None),
            MakeMoneyColumn(nameof(CartItem.UnitPrice), "Unit Price", 110),
            MakeMoneyColumn(nameof(CartItem.LineTotal), "Line Total", 120));

        totalsPanel.Height = 330;
        discountBox.Width = 270;
        discountTypeCombo.Width = 170;
        discountValueNumeric.Width = 170;
        applyDiscountButton.Width = 170;
        discountNoteLabel.Width = 240;
        discountBox.Text = "Discount (USD) and exchange rate";

        var rateLabel = new Label
        {
            AutoSize = true,
            Location = new Point(14, 219),
            Text = "KHR per $1"
        };
        _rateNumeric = new NumericUpDown
        {
            Location = new Point(130, 215),
            Size = new Size(110, 29),
            Minimum = 1m,
            Maximum = 100_000m,
            ThousandsSeparator = true,
            Value = UiTheme.KhrPerUsd
        };
        _rateNumeric.ValueChanged += (_, _) =>
        {
            UiTheme.KhrPerUsd = _rateNumeric.Value;
            UpdateTotals();
        };
        discountBox.Controls.Add(rateLabel);
        discountBox.Controls.Add(_rateNumeric);

        paidPanel.Height = 100;
        paidLabel.Text = "Paid USD";
        amountPaidTextBox.Location = new Point(120, 3);
        amountPaidTextBox.Width = Math.Max(80, paidPanel.Width - 120);
        paidPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = paidLabel.Font,
            Location = new Point(2, 53),
            Text = "Paid KHR"
        });
        _khrPaidTextBox = new TextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = amountPaidTextBox.Font,
            Location = new Point(120, 47),
            Size = new Size(Math.Max(80, paidPanel.Width - 120), 39),
            Text = "0",
            TextAlign = HorizontalAlignment.Right
        };
        _khrPaidTextBox.TextChanged += AmountPaidTextBox_TextChanged;
        paidPanel.Controls.Add(_khrPaidTextBox);

        summaryTable.RowCount = 6;
        summaryTable.RowStyles.Clear();
        for (var row = 0; row < 6; row++)
        {
            summaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));
        }
        summaryTable.SetRow(changeCaptionLabel, 4);
        summaryTable.SetRow(changeValueLabel, 4);
        var khrTotalCaption = new Label();
        _khrTotalValueLabel = new Label();
        _khrBalanceCaptionLabel = new Label();
        _khrBalanceValueLabel = new Label();
        summaryTable.Controls.Add(khrTotalCaption, 0, 3);
        summaryTable.Controls.Add(_khrTotalValueLabel, 1, 3);
        summaryTable.Controls.Add(_khrBalanceCaptionLabel, 0, 5);
        summaryTable.Controls.Add(_khrBalanceValueLabel, 1, 5);

        ConfigureSummaryCaption(subtotalCaptionLabel, "Subtotal USD");
        ConfigureSummaryCaption(discountCaptionLabel, "Discount USD");
        ConfigureSummaryCaption(totalCaptionLabel, "Total USD");
        ConfigureSummaryCaption(khrTotalCaption, "Total KHR");
        ConfigureSummaryCaption(changeCaptionLabel, "Still Owing USD");
        ConfigureSummaryCaption(_khrBalanceCaptionLabel, "Still Owing KHR");
        ConfigureSummaryValue(subtotalValueLabel, UiTheme.BodyFont);
        ConfigureSummaryValue(discountValueLabel2, UiTheme.BodyFont);
        ConfigureSummaryValue(totalValueLabel, totalCaptionLabel.Font);
        ConfigureSummaryValue(_khrTotalValueLabel, UiTheme.BodyFont);
        ConfigureSummaryValue(changeValueLabel, changeCaptionLabel.Font);
        ConfigureSummaryValue(_khrBalanceValueLabel, UiTheme.BodyFont);

        discountTypeCombo.SelectedIndex = 0;
    }

    private async void PosForm_Load(object? sender, EventArgs e)
    {
        cashierLabel.Text = $"Signed in as {_cashier.Username} ({_cashier.Role})";
        RefreshCart();
        await LoadProductsAsync().ConfigureAwait(true);
        searchTextBox.Focus();
    }

    // ── Product search ───────────────────────────────────────────────────────

    private async Task LoadProductsAsync()
    {
        var version = ++_productSearchVersion;
        var search = searchTextBox.Text;
        var products = await AsyncUi.RunAsync(
            this,
            "load the product list",
            () => _services.Inventory.SearchAsync(search)).ConfigureAwait(true);

        if (products is null || version != _productSearchVersion || IsDisposed)
        {
            return;
        }

        productGrid.DataSource = products.ToList();
        statusLabel.Text = $"{products.Count} product(s) listed";
    }

    private async void SearchTextBox_TextChanged(object? sender, EventArgs e) =>
        await LoadProductsAsync().ConfigureAwait(true);

    private async void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;

        var code = searchTextBox.Text.Trim();
        if (code.Length == 0)
        {
            return;
        }

        var product = await AsyncUi.RunAsync(
            this,
            "look up that product code",
            () => _services.Inventory.GetBySkuAsync(code)).ConfigureAwait(true);

        if (product is null)
        {
            return;
        }

        AddProductToCart(product, (int)quantityNumeric.Value);
        searchTextBox.Clear();
        searchTextBox.Focus();
    }

    private void ProductGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            AddSelectedProductToCart();
        }
    }

    // ── Cart management ──────────────────────────────────────────────────────

    private void AddToCartButton_Click(object? sender, EventArgs e) => AddSelectedProductToCart();

    private void AddSelectedProductToCart()
    {
        if (productGrid.CurrentRow?.DataBoundItem is not Product product)
        {
            UiFeedback.ShowBusinessRule(this, "Select a product from the list first.");
            return;
        }

        AddProductToCart(product, (int)quantityNumeric.Value);
    }

    private void AddProductToCart(Product product, int quantity)
    {
        try
        {
            _cart.AddItem(product, quantity);
            quantityNumeric.Value = 1; 
            RefreshCart();
            statusLabel.Text = $"Added {quantity} × {product.Name}";
        }
        catch (BusinessRuleException ex)
        {
            UiFeedback.ShowBusinessRule(this, ex.Message);
        }
    }

    private void ChangeQuantityButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedCartLine() is not { } line)
        {
            return;
        }

        using var prompt = new QuantityPromptForm(line.ProductName, line.Quantity, line.Product.StockQuantity);

        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _cart.UpdateQuantity(line.ProductId, prompt.Quantity);
            RefreshCart();
        }
        catch (BusinessRuleException ex)
        {
            UiFeedback.ShowBusinessRule(this, ex.Message);
        }
    }

    private void RemoveLineButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedCartLine() is not { } line)
        {
            return;
        }

        _cart.RemoveItem(line.ProductId);
        RefreshCart();
        statusLabel.Text = $"Removed {line.ProductName}";
    }

    private void ClearCartButton_Click(object? sender, EventArgs e)
    {
        if (_cart.IsEmpty)
        {
            return;
        }

        if (!UiFeedback.Confirm(this, "Clear every item from the current sale?", "Clear cart"))
        {
            return;
        }

        _cart.Clear();
        ResetPaymentAndDiscount();
        RefreshCart();
        statusLabel.Text = "Cart cleared";
    }

    private CartItem? GetSelectedCartLine()
    {
        if (cartGrid.CurrentRow?.DataBoundItem is CartItem line)
        {
            return line;
        }

        UiFeedback.ShowBusinessRule(this, "Select a line in the cart first.");
        return null;
    }

    private void RefreshCart()
    {
        cartGrid.DataSource = null;
        cartGrid.DataSource = _cart.Items.ToList();
        UpdateTotals();
    }

    // ── Discounts and totals ─────────────────────────────────────────────────

    private void DiscountTypeCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        discountValueNumeric.Enabled = discountTypeCombo.SelectedIndex > 0;
        discountValueNumeric.Maximum = discountTypeCombo.SelectedIndex == 1 ? 100 : 100000;
        discountValueLabel.Text = discountTypeCombo.SelectedIndex == 1 ? "Rate" : "Value";

        if (discountTypeCombo.SelectedIndex == 0)
        {
            discountValueNumeric.Value = 0;
        }
    }

    private void ApplyDiscountButton_Click(object? sender, EventArgs e)
    {
        try
        {
            DiscountStrategy strategy = discountTypeCombo.SelectedIndex switch
            {
                1 => new PercentageDiscount(discountValueNumeric.Value),
                2 => new FlatDiscount(discountValueNumeric.Value),
                _ => NoDiscount.Instance
            };

            _saleService.ApplyDiscount(strategy);
            discountNoteLabel.Text = $"Currently applied: {strategy.Description}";
            UpdateTotals();
        }
        catch (BusinessRuleException ex)
        {
            UiFeedback.ShowBusinessRule(this, ex.Message);
        }
    }

    private void AmountPaidTextBox_TextChanged(object? sender, EventArgs e) => UpdateTotals();

    private void UpdateTotals()
    {
        var quote = _saleService.QuoteCart(_cart);
        var validPayment = TryReadTender(quote.Total, out var tender);

        subtotalValueLabel.Text = UiTheme.Money(quote.Subtotal);
        discountValueLabel2.Text = quote.DiscountAmount > 0m
            ? $"− {UiTheme.Money(quote.DiscountAmount)}"
            : UiTheme.Money(0m);
        totalValueLabel.Text = UiTheme.Money(quote.Total);
        _khrTotalValueLabel.Text = UiTheme.MoneyKhr(tender.TotalKhr);

        if (!validPayment)
        {
            changeCaptionLabel.Text = "Payment";
            changeValueLabel.Text = "Check amounts";
            _khrBalanceCaptionLabel.Text = "KHR balance";
            _khrBalanceValueLabel.Text = "—";
            changeValueLabel.ForeColor = UiTheme.Danger;
            _khrBalanceValueLabel.ForeColor = UiTheme.Danger;
        }
        else if (tender.IsSufficient)
        {
            changeCaptionLabel.Text = "Change USD";
            changeValueLabel.Text = UiTheme.Money(tender.AmountPaidUsd - quote.Total);
            changeValueLabel.ForeColor = UiTheme.Success;
            _khrBalanceCaptionLabel.Text = "Change KHR";
            _khrBalanceValueLabel.Text = UiTheme.MoneyKhr(tender.ChangeKhr);
            _khrBalanceValueLabel.ForeColor = UiTheme.Success;
        }
        else
        {
            changeCaptionLabel.Text = "Still Owing USD";
            changeValueLabel.Text = UiTheme.Money(decimal.Ceiling(tender.OutstandingKhr / tender.KhrPerUsd * 100m) / 100m);
            changeValueLabel.ForeColor = UiTheme.Danger;
            _khrBalanceCaptionLabel.Text = "Still Owing KHR";
            _khrBalanceValueLabel.Text = UiTheme.MoneyKhr(tender.OutstandingKhr);
            _khrBalanceValueLabel.ForeColor = UiTheme.Danger;
        }

        checkoutButton.Enabled = !_cart.IsEmpty && validPayment;
    }

    private bool TryReadTender(decimal totalUsd, out CheckoutTender tender)
    {
        var validUsd = decimal.TryParse(amountPaidTextBox.Text, out var usdPaid) &&
            usdPaid >= 0m && usdPaid == decimal.Round(usdPaid, 2);
        var validKhr = decimal.TryParse(_khrPaidTextBox.Text, out var khrPaid) &&
            khrPaid >= 0m && khrPaid == decimal.Truncate(khrPaid);
        tender = new CheckoutTender(
            totalUsd,
            validUsd ? usdPaid : 0m,
            validKhr ? khrPaid : 0m,
            _rateNumeric.Value);
        return validUsd && validKhr;
    }

    // ── Checkout ─────────────────────────────────────────────────────────────

    private async void CheckoutButton_Click(object? sender, EventArgs e)
    {
        var total = _saleService.QuoteCart(_cart).Total;
        if (!TryReadTender(total, out var tender))
        {
            UiFeedback.ShowBusinessRule(this, "Enter a valid USD amount and a whole, nonnegative KHR amount.");
            return;
        }

        if (!tender.IsSufficient)
        {
            UiFeedback.ShowBusinessRule(this, $"Collect another {UiTheme.MoneyKhr(tender.OutstandingKhr)} before checkout.");
            return;
        }

        checkoutButton.Enabled = false;
        try
        {
            var sale = await AsyncUi.RunAsync(
                this,
                "complete the sale",
                () => _saleService.CheckoutAsync(_cart, _cashier.UserId, tender.AmountPaidUsd)).ConfigureAwait(true);

            if (sale is null)
            {
                // The failure has already been reported with the right message for its kind:
                // a business rule violation, or a system error telling the cashier to retry.
                return;
            }

            using (var receipt = new ReceiptForm(sale, _cashier, tender))
            {
                receipt.ShowDialog(this);
            }

            ResetForNextCustomer();
            statusLabel.Text = $"Sale #{sale.SaleId} completed — change due {UiTheme.MoneyKhr(tender.ChangeKhr)}";
            await LoadProductsAsync().ConfigureAwait(true);
        }
        finally
        {
            if (!IsDisposed)
            {
                UpdateTotals();
            }
        }
    }

    private void ResetForNextCustomer()
    {
        _cart.Clear();
        ResetPaymentAndDiscount();
        searchTextBox.Clear();
        RefreshCart();
        searchTextBox.Focus();
    }

    private void ResetPaymentAndDiscount()
    {
        _saleService.ApplyDiscount(null);
        discountTypeCombo.SelectedIndex = 0;
        discountValueNumeric.Value = 0;
        discountNoteLabel.Text = "Currently applied: No discount";
        amountPaidTextBox.Text = "0.00";
        _khrPaidTextBox.Text = "0";
    }

    // ── Shell ────────────────────────────────────────────────────────────────

    private void PosForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F9 when checkoutButton.Enabled:
                e.SuppressKeyPress = true;
                CheckoutButton_Click(sender, EventArgs.Empty);
                break;

            case Keys.F2:
                e.SuppressKeyPress = true;
                searchTextBox.Focus();
                searchTextBox.SelectAll();
                break;
        }
    }

    private void LogoutButton_Click(object? sender, EventArgs e)
    {
        if (!_cart.IsEmpty &&
            !UiFeedback.Confirm(this, "The current sale has not been completed. Sign out and discard it?", "Sign out"))
        {
            return;
        }

        LogOutRequested = true;
        Close();
    }
}

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

    public bool LogOutRequested { get; private set; }

    public PosForm(AppServices services, User cashier)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _cashier = cashier ?? throw new ArgumentNullException(nameof(cashier));
        _saleService = services.CreateSaleService();

        InitializeComponent();
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
        var products = await AsyncUi.RunAsync(
            this,
            "load the product list",
            () => _services.Inventory.SearchAsync(searchTextBox.Text)).ConfigureAwait(true);

        if (products is null)
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
        var quote = _saleService.QuoteCart(_cart, ReadAmountPaid());

        subtotalValueLabel.Text = UiTheme.Money(quote.Subtotal);
        discountValueLabel2.Text = quote.DiscountAmount > 0m
            ? $"− {UiTheme.Money(quote.DiscountAmount)}"
            : UiTheme.Money(0m);
        totalValueLabel.Text = UiTheme.Money(quote.Total);

        if (quote.IsPaymentSufficient)
        {
            changeCaptionLabel.Text = "Change Due";
            changeValueLabel.Text = UiTheme.Money(quote.ChangeDue);
            changeValueLabel.ForeColor = UiTheme.Success;
        }
        else
        {
            changeCaptionLabel.Text = "Still Owing";
            changeValueLabel.Text = UiTheme.Money(quote.AmountOutstanding);
            changeValueLabel.ForeColor = UiTheme.Danger;
        }

        checkoutButton.Enabled = !_cart.IsEmpty;
    }

    private decimal ReadAmountPaid() =>
        decimal.TryParse(amountPaidTextBox.Text, out var amount) && amount >= 0m ? amount : 0m;

    // ── Checkout ─────────────────────────────────────────────────────────────

    private async void CheckoutButton_Click(object? sender, EventArgs e)
    {
        var amountPaid = ReadAmountPaid();

        checkoutButton.Enabled = false;
        try
        {
            var sale = await AsyncUi.RunAsync(
                this,
                "complete the sale",
                () => _saleService.CheckoutAsync(_cart, _cashier.UserId, amountPaid)).ConfigureAwait(true);

            if (sale is null)
            {
                // The failure has already been reported with the right message for its kind:
                // a business rule violation, or a system error telling the cashier to retry.
                return;
            }

            using (var receipt = new ReceiptForm(sale, _cashier))
            {
                receipt.ShowDialog(this);
            }

            ResetForNextCustomer();
            statusLabel.Text = $"Sale #{sale.SaleId} completed — change due {UiTheme.Money(sale.ChangeDue)}";
            await LoadProductsAsync().ConfigureAwait(true);
        }
        finally
        {
            if (!IsDisposed)
            {
                checkoutButton.Enabled = !_cart.IsEmpty;
            }
        }
    }

    private void ResetForNextCustomer()
    {
        _cart.Clear();
        _saleService.ApplyDiscount(null);
        discountTypeCombo.SelectedIndex = 0;
        discountValueNumeric.Value = 0;
        discountNoteLabel.Text = "Currently applied: No discount";
        amountPaidTextBox.Text = "0.00";
        searchTextBox.Clear();
        RefreshCart();
        searchTextBox.Focus();
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

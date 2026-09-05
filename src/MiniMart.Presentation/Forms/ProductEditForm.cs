using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class ProductEditForm : Form
{
    private readonly AppServices _services;
    private readonly Product? _existing;

    private TextBox _nameTextBox = null!;
    private TextBox _skuTextBox = null!;
    private NumericUpDown _unitPriceNumeric = null!;
    private NumericUpDown _costPriceNumeric = null!;
    private NumericUpDown _stockNumeric = null!;
    private NumericUpDown _reorderNumeric = null!;
    private ComboBox _categoryCombo = null!;
    private CheckBox _activeCheckBox = null!;
    private Button _saveButton = null!;

    private bool IsNew => _existing is null;

    public ProductEditForm(AppServices services, Product? product)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _existing = product;

        BuildUi();
        Load += async (_, _) => await LoadCategoriesAsync().ConfigureAwait(true);
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
            Text = IsNew ? "New Product" : "Edit Product"
        });

        var y = 74;
        const int labelX = 24;
        const int fieldX = 150;
        const int fieldWidth = 280;
        const int rowHeight = 40;

        _nameTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = Product.NameMaxLength,
            Size = new Size(fieldWidth, 25)
        };
        AddRow("Name", _nameTextBox, labelX, ref y, rowHeight);

        _skuTextBox = new TextBox
        {
            Location = new Point(fieldX, y),
            MaxLength = Product.SkuMaxLength,
            Size = new Size(fieldWidth, 25)
        };
        AddRow("Code / SKU", _skuTextBox, labelX, ref y, rowHeight);

        _categoryCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(fieldX, y),
            Size = new Size(fieldWidth, 25)
        };
        AddRow("Category", _categoryCombo, labelX, ref y, rowHeight);

        _unitPriceNumeric = MakeMoneyNumeric(fieldX, y, fieldWidth);
        AddRow("Unit Price", _unitPriceNumeric, labelX, ref y, rowHeight);

        _costPriceNumeric = MakeMoneyNumeric(fieldX, y, fieldWidth);
        AddRow("Cost Price", _costPriceNumeric, labelX, ref y, rowHeight);

        _stockNumeric = new NumericUpDown
        {
            Location = new Point(fieldX, y),
            Maximum = 1_000_000,
            Size = new Size(fieldWidth, 25)
        };
        AddRow(IsNew ? "Opening Stock" : "Stock On Hand", _stockNumeric, labelX, ref y, rowHeight);

        _reorderNumeric = new NumericUpDown
        {
            Location = new Point(fieldX, y),
            Maximum = 1_000_000,
            Size = new Size(fieldWidth, 25),
            Value = 5
        };
        AddRow("Reorder Level", _reorderNumeric, labelX, ref y, rowHeight);

        _activeCheckBox = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Location = new Point(fieldX, y + 4),
            Text = "Available for sale"
        };
        Controls.Add(_activeCheckBox);
        y += rowHeight;

        if (!IsNew)
        {
            // Stock is changed through the guarded Adjust Stock action so that every change
            // goes through Product.AdjustStockTo rather than being edited alongside prices.
            _stockNumeric.Enabled = false;
            Controls.Add(new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.MutedText,
                Location = new Point(fieldX, y),
                Size = new Size(fieldWidth, 32),
                Text = "Use “Adjust Stock” on the product list to change the quantity on hand."
            });
            y += 34;
        }

        _saveButton = new Button
        {
            Location = new Point(fieldX + fieldWidth - 200, y + 10),
            Size = new Size(96, 36),
            Text = "Save"
        };
        UiTheme.StyleButton(_saveButton, UiTheme.Success);
        _saveButton.Click += async (_, _) => await SaveAsync().ConfigureAwait(true);

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(fieldX + fieldWidth - 96, y + 10),
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
        ClientSize = new Size(470, y + 66);
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = IsNew ? "New Product" : $"Edit Product — {_existing!.Name}";

        ResumeLayout(false);
        PerformLayout();
    }

    private void AddRow(string caption, Control field, int labelX, ref int y, int rowHeight)
    {
        Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(labelX, y + 4),
            Text = caption
        });
        Controls.Add(field);
        y += rowHeight;
    }

    private static NumericUpDown MakeMoneyNumeric(int x, int y, int width) =>
        new()
        {
            DecimalPlaces = 2,
            Location = new Point(x, y),
            Maximum = 99_999_999m,
            Size = new Size(width, 25),
            ThousandsSeparator = true
        };

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

        if (categories.Count == 0)
        {
            UiFeedback.ShowBusinessRule(
                this,
                "There are no categories yet. Create at least one category before adding products.");
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        _categoryCombo.DisplayMember = nameof(Category.Name);
        _categoryCombo.ValueMember = nameof(Category.CategoryId);
        _categoryCombo.DataSource = categories.ToList();

        if (_existing is not null)
        {
            _nameTextBox.Text = _existing.Name;
            _skuTextBox.Text = _existing.Sku;
            _unitPriceNumeric.Value = _existing.UnitPrice;
            _costPriceNumeric.Value = _existing.CostPrice;
            _stockNumeric.Value = _existing.StockQuantity;
            _reorderNumeric.Value = _existing.ReorderLevel;
            _activeCheckBox.Checked = _existing.IsActive;
            _categoryCombo.SelectedValue = _existing.CategoryId;
        }

        _nameTextBox.Focus();
    }

    private async Task SaveAsync()
    {
        if (_categoryCombo.SelectedValue is not int categoryId)
        {
            UiFeedback.ShowBusinessRule(this, "Please choose a category.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            UiFeedback.ShowBusinessRule(this, "Please enter a product name.");
            _nameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_skuTextBox.Text))
        {
            UiFeedback.ShowBusinessRule(this, "Please enter a product code.");
            _skuTextBox.Focus();
            return;
        }

        _saveButton.Enabled = false;

        var succeeded = await AsyncUi.RunAsync(this, "save the product", async () =>
        {
            if (IsNew)
            {
                await _services.Inventory.CreateProductAsync(
                    _nameTextBox.Text,
                    _skuTextBox.Text,
                    _unitPriceNumeric.Value,
                    _costPriceNumeric.Value,
                    (int)_stockNumeric.Value,
                    (int)_reorderNumeric.Value,
                    categoryId).ConfigureAwait(true);
            }
            else
            {
                await _services.Inventory.UpdateProductAsync(
                    _existing!.ProductId,
                    _nameTextBox.Text,
                    _skuTextBox.Text,
                    _unitPriceNumeric.Value,
                    _costPriceNumeric.Value,
                    (int)_reorderNumeric.Value,
                    categoryId,
                    _activeCheckBox.Checked).ConfigureAwait(true);
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

using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class StockAdjustmentForm : Form
{
    private readonly NumericUpDown _quantityNumeric;

    public int NewQuantity => (int)_quantityNumeric.Value;

    public StockAdjustmentForm(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 52 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = Color.White,
            Location = new Point(18, 13),
            Text = "Adjust Stock"
        });

        var productLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 11f),
            Location = new Point(22, 70),
            Size = new Size(360, 24),
            Text = product.Name
        };

        var currentLabel = new Label
        {
            AutoSize = false,
            ForeColor = UiTheme.MutedText,
            Location = new Point(22, 96),
            Size = new Size(360, 22),
            Text = $"Code {product.Sku} — currently {product.StockQuantity} on hand"
        };

        var promptLabel = new Label
        {
            AutoSize = true,
            Location = new Point(22, 132),
            Text = "Counted quantity"
        };

        _quantityNumeric = new NumericUpDown
        {
            Font = new Font("Segoe UI", 14f),
            Location = new Point(22, 156),
            Maximum = 1_000_000,
            Minimum = 0,
            Size = new Size(360, 34),
            TextAlign = HorizontalAlignment.Right,
            Value = product.StockQuantity
        };

        var okButton = new Button { Location = new Point(190, 208), Size = new Size(96, 36), Text = "Save" };
        UiTheme.StyleButton(okButton, UiTheme.Success);
        okButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(292, 208),
            Size = new Size(90, 36),
            Text = "Cancel"
        };
        UiTheme.StyleButton(cancelButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));

        AcceptButton = okButton;
        CancelButton = cancelButton;
        BackColor = Color.White;
        ClientSize = new Size(404, 264);
        Controls.AddRange(new Control[]
        {
            productLabel, currentLabel, promptLabel, _quantityNumeric, okButton, cancelButton, headerPanel
        });
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Adjust Stock";
    }
}

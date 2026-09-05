using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class QuantityPromptForm : Form
{
    private readonly NumericUpDown _quantityNumeric;

    public int Quantity => (int)_quantityNumeric.Value;

    public QuantityPromptForm(string productName, int currentQuantity, int maximumQuantity)
    {
        var promptLabel = new Label
        {
            AutoSize = false,
            Font = UiTheme.BodyFont,
            Location = new Point(18, 16),
            Size = new Size(330, 40),
            Text = $"New quantity for {productName}:"
        };

        _quantityNumeric = new NumericUpDown
        {
            Font = new Font("Segoe UI", 14f),
            Location = new Point(18, 60),
            Minimum = 1,
            Maximum = Math.Max(1, maximumQuantity),
            Value = Math.Clamp(currentQuantity, 1, Math.Max(1, maximumQuantity)),
            Size = new Size(330, 34),
            TextAlign = HorizontalAlignment.Right
        };

        var availableLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = UiTheme.MutedText,
            Location = new Point(20, 98),
            Text = $"{maximumQuantity} in stock"
        };

        var okButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(190, 126),
            Size = new Size(76, 34),
            Text = "OK"
        };
        UiTheme.StyleButton(okButton, UiTheme.Primary);

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(272, 126),
            Size = new Size(76, 34),
            Text = "Cancel"
        };
        UiTheme.StyleButton(cancelButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));

        AcceptButton = okButton;
        CancelButton = cancelButton;
        BackColor = Color.White;
        ClientSize = new Size(366, 176);
        Controls.AddRange(new Control[] { promptLabel, _quantityNumeric, availableLabel, okButton, cancelButton });
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Change Quantity";
    }
}

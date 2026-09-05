using System.Configuration;
using System.Drawing.Printing;
using System.Text;
using MiniMart.BusinessLogic.Models;
using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class ReceiptForm : Form
{
    private const int ReceiptWidth = 42;

    private readonly Sale _sale;
    private readonly string _receiptText;
    private readonly TextBox _receiptTextBox;

    public ReceiptForm(Sale sale, User cashier)
    {
        _sale = sale ?? throw new ArgumentNullException(nameof(sale));
        ArgumentNullException.ThrowIfNull(cashier);

        _receiptText = BuildReceiptText(sale, cashier);

        var headerPanel = new Panel
        {
            BackColor = UiTheme.Success,
            Dock = DockStyle.Top,
            Height = 92
        };

        var headlineLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15f),
            ForeColor = Color.White,
            Location = new Point(18, 14),
            Text = $"Sale #{sale.SaleId} completed"
        };

        var changeLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19f),
            ForeColor = Color.White,
            Location = new Point(18, 46),
            Text = $"Change due: {UiTheme.Money(sale.ChangeDue)}"
        };

        headerPanel.Controls.Add(changeLabel);
        headerPanel.Controls.Add(headlineLabel);

        _receiptTextBox = new TextBox
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Font = UiTheme.ReceiptFont,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = _receiptText,
            WordWrap = false
        };

        var contentPanel = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 12, 18, 12)
        };
        contentPanel.Controls.Add(_receiptTextBox);

        var printButton = new Button { Text = "Print…", Size = new Size(110, 36), Location = new Point(18, 10) };
        UiTheme.StyleButton(printButton, UiTheme.Primary);
        printButton.Click += (_, _) => ShowPrintPreview();

        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            DialogResult = DialogResult.OK,
            Location = new Point(310, 10),
            Size = new Size(140, 36),
            Text = "Next Customer"
        };
        UiTheme.StyleButton(closeButton, UiTheme.Success);

        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 56 };
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(printButton);

        AcceptButton = closeButton;
        BackColor = Color.White;
        ClientSize = new Size(480, 640);
        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);
        Controls.Add(headerPanel);
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = $"Receipt — Sale #{sale.SaleId}";
    }

    private static string BuildReceiptText(Sale sale, User cashier)
    {
        var storeName = ConfigurationManager.AppSettings["Store.Name"] ?? "Mini Mart";
        var storeAddress = ConfigurationManager.AppSettings["Store.Address"] ?? string.Empty;
        var storePhone = ConfigurationManager.AppSettings["Store.Phone"] ?? string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine(Centre(storeName.ToUpperInvariant()));

        if (!string.IsNullOrWhiteSpace(storeAddress))
        {
            builder.AppendLine(Centre(storeAddress));
        }

        if (!string.IsNullOrWhiteSpace(storePhone))
        {
            builder.AppendLine(Centre(storePhone));
        }

        builder.AppendLine();
        builder.AppendLine(new string('=', ReceiptWidth));
        builder.AppendLine($"Sale No : {sale.SaleId}");
        builder.AppendLine($"Date    : {sale.SaleDate:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Cashier : {sale.CashierName ?? cashier.Username}");
        builder.AppendLine(new string('=', ReceiptWidth));
        builder.AppendLine();

        foreach (var line in sale.Lines)
        {
            builder.AppendLine(Truncate(line.ProductName ?? $"Product #{line.ProductId}", ReceiptWidth));
            var detail = $"  {line.Quantity} x {line.UnitPrice:N2}";
            builder.AppendLine(AmountRow(detail, line.LineTotal));
        }

        builder.AppendLine();
        builder.AppendLine(new string('-', ReceiptWidth));
        builder.AppendLine(AmountRow("Subtotal", sale.Subtotal));

        if (sale.DiscountAmount > 0m)
        {
            builder.AppendLine(AmountRow("Discount", -sale.DiscountAmount));
        }

        builder.AppendLine(AmountRow("TOTAL", sale.TotalAmount));
        builder.AppendLine(AmountRow("Cash Paid", sale.AmountPaid));
        builder.AppendLine(AmountRow("Change Due", sale.ChangeDue));
        builder.AppendLine(new string('-', ReceiptWidth));
        builder.AppendLine();
        builder.AppendLine(Centre($"Items: {sale.TotalUnits}"));
        builder.AppendLine();
        builder.AppendLine(Centre("Thank you for shopping with us!"));
        builder.AppendLine(Centre("Please keep this receipt."));

        return builder.ToString();
    }

    private static string AmountRow(string caption, decimal amount) =>
        caption.PadRight(ReceiptWidth - 12) + $"{amount,12:N2}";

    private static string Centre(string text)
    {
        var trimmed = Truncate(text, ReceiptWidth);
        var padding = Math.Max(0, (ReceiptWidth - trimmed.Length) / 2);
        return new string(' ', padding) + trimmed;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];

    private void ShowPrintPreview()
    {
        using var document = new PrintDocument { DocumentName = $"MiniMart Receipt {_sale.SaleId}" };
        var lines = _receiptText.Replace("\r\n", "\n").Split('\n');
        var lineIndex = 0;

        document.PrintPage += (_, args) =>
        {
            using var font = new Font("Consolas", 9f);
            float y = args.MarginBounds.Top;
            var lineHeight = font.GetHeight(args.Graphics!);

            while (lineIndex < lines.Length && y + lineHeight < args.MarginBounds.Bottom)
            {
                args.Graphics!.DrawString(lines[lineIndex], font, Brushes.Black, args.MarginBounds.Left, y);
                y += lineHeight;
                lineIndex++;
            }

            args.HasMorePages = lineIndex < lines.Length;
        };

        try
        {
            using var preview = new PrintPreviewDialog
            {
                Document = document,
                StartPosition = FormStartPosition.CenterParent,
                Width = 620,
                Height = 780
            };

            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            // No printer configured is a normal situation on a development machine, and it
            // must never obscure the fact that the sale itself was saved successfully.
            UiFeedback.ShowError(this, ex, "open the print preview");
        }
    }
}

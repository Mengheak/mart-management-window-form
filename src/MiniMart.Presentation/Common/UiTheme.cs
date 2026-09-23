using System.Configuration;
using System.Globalization;

namespace MiniMart.Presentation.Common;

public static class UiTheme
{
    public static readonly Color Primary = Color.FromArgb(31, 58, 95);

    public static readonly Color PrimaryLight = Color.FromArgb(46, 86, 138);

    public static readonly Color Success = Color.FromArgb(24, 128, 76);

    public static readonly Color Danger = Color.FromArgb(178, 46, 46);

    public static readonly Color Warning = Color.FromArgb(255, 244, 214);

    public static readonly Color Surface = Color.FromArgb(245, 247, 250);

    public static readonly Color MutedText = Color.FromArgb(105, 115, 130);

    public static readonly Font BodyFont = new("Segoe UI", 9.75f);

    public static readonly Font HeadingFont = new("Segoe UI Semibold", 15f);

    public static readonly Font TotalFont = new("Segoe UI", 22f, FontStyle.Bold);

    public static readonly Font ReceiptFont = new("Consolas", 9.5f);

    public static string CurrencySymbol => "$";

    public static string Money(decimal amount) => $"{CurrencySymbol}{amount:N2}";

    public static string GridMoneyFormat => $"\"{CurrencySymbol.Replace("\"", string.Empty)}\"#,##0.00";

    public static string CurrencyCode => "USD";

    public static decimal KhrPerUsd { get; set; } = ReadDefaultKhrPerUsd();

    public static string MoneyKhr(decimal amount) => $"៛{decimal.Round(amount, 0, MidpointRounding.AwayFromZero):N0}";

    private static decimal ReadDefaultKhrPerUsd() =>
        decimal.TryParse(ConfigurationManager.AppSettings["Ui.KhrPerUsd"], NumberStyles.Number,
            CultureInfo.InvariantCulture, out var rate) && rate >= 1m && rate <= 100_000m &&
            rate == decimal.Truncate(rate)
            ? rate
            : 4_150m;

    public static void StyleButton(Button button, Color background, Color? foreground = null)
    {
        ArgumentNullException.ThrowIfNull(button);

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = background;
        button.ForeColor = foreground ?? Color.White;
        button.Font = new Font("Segoe UI Semibold", 9.75f);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleGrid(DataGridView grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.75f);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 28;
        grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }
}

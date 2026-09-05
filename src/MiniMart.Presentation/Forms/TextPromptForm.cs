using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

public sealed class TextPromptForm : Form
{
    private readonly TextBox _valueTextBox;

    public string Value => _valueTextBox.Text.Trim();

    public TextPromptForm(string title, string caption, string initialValue, int maxLength)
    {
        var headerPanel = new Panel { BackColor = UiTheme.Primary, Dock = DockStyle.Top, Height = 48 };
        headerPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = Color.White,
            Location = new Point(18, 12),
            Text = title
        });

        var captionLabel = new Label
        {
            AutoSize = true,
            Location = new Point(22, 66),
            Text = caption
        };

        _valueTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 12f),
            Location = new Point(22, 90),
            MaxLength = maxLength,
            Size = new Size(340, 29),
            Text = initialValue
        };

        var okButton = new Button { Location = new Point(170, 136), Size = new Size(96, 36), Text = "Save" };
        UiTheme.StyleButton(okButton, UiTheme.Success);
        okButton.Click += (_, _) =>
        {
            if (Value.Length == 0)
            {
                UiFeedback.ShowBusinessRule(this, $"Please enter a {caption.ToLowerInvariant()}.");
                _valueTextBox.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(272, 136),
            Size = new Size(90, 36),
            Text = "Cancel"
        };
        UiTheme.StyleButton(cancelButton, Color.FromArgb(226, 230, 236), Color.FromArgb(60, 70, 85));

        AcceptButton = okButton;
        CancelButton = cancelButton;
        BackColor = Color.White;
        ClientSize = new Size(384, 192);
        Controls.AddRange(new Control[] { captionLabel, _valueTextBox, okButton, cancelButton, headerPanel });
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = title;

        Shown += (_, _) => _valueTextBox.SelectAll();
    }
}

using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

partial class PosForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel headerPanel;
    private FlowLayoutPanel headerRightPanel;
    private Label headerTitleLabel;
    private Label cashierLabel;
    private Button logoutButton;

    private Panel leftPanel;
    private Panel searchPanel;
    private Label searchLabel;
    private TextBox searchTextBox;
    private DataGridView productGrid;
    private Panel addPanel;
    private Label quantityLabel;
    private NumericUpDown quantityNumeric;
    private Button addToCartButton;

    private Panel rightPanel;
    private Panel cartToolbarPanel;
    private Label cartTitleLabel;
    private Button changeQuantityButton;
    private Button removeLineButton;
    private Button clearCartButton;
    private DataGridView cartGrid;

    private Panel totalsPanel;
    private GroupBox discountBox;
    private Label discountTypeLabel;
    private ComboBox discountTypeCombo;
    private Label discountValueLabel;
    private NumericUpDown discountValueNumeric;
    private Button applyDiscountButton;
    private Label discountNoteLabel;

    private Panel summaryPanel;
    private TableLayoutPanel summaryTable;
    private Label subtotalCaptionLabel;
    private Label subtotalValueLabel;
    private Label discountCaptionLabel;
    private Label discountValueLabel2;
    private Label totalCaptionLabel;
    private Label totalValueLabel;
    private Label changeCaptionLabel;
    private Label changeValueLabel;
    private Panel paidPanel;
    private Label paidLabel;
    private TextBox amountPaidTextBox;
    private Button checkoutButton;

    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        headerRightPanel = new FlowLayoutPanel();
        logoutButton = new Button();
        cashierLabel = new Label();
        headerTitleLabel = new Label();
        leftPanel = new Panel();
        productGrid = new DataGridView();
        addPanel = new Panel();
        addToCartButton = new Button();
        quantityNumeric = new NumericUpDown();
        quantityLabel = new Label();
        searchPanel = new Panel();
        searchTextBox = new TextBox();
        searchLabel = new Label();
        rightPanel = new Panel();
        cartGrid = new DataGridView();
        totalsPanel = new Panel();
        summaryPanel = new Panel();
        summaryTable = new TableLayoutPanel();
        subtotalCaptionLabel = new Label();
        subtotalValueLabel = new Label();
        discountCaptionLabel = new Label();
        discountValueLabel2 = new Label();
        totalCaptionLabel = new Label();
        totalValueLabel = new Label();
        changeCaptionLabel = new Label();
        changeValueLabel = new Label();
        paidPanel = new Panel();
        amountPaidTextBox = new TextBox();
        paidLabel = new Label();
        checkoutButton = new Button();
        discountBox = new GroupBox();
        discountNoteLabel = new Label();
        applyDiscountButton = new Button();
        discountValueNumeric = new NumericUpDown();
        discountValueLabel = new Label();
        discountTypeCombo = new ComboBox();
        discountTypeLabel = new Label();
        cartToolbarPanel = new Panel();
        clearCartButton = new Button();
        removeLineButton = new Button();
        changeQuantityButton = new Button();
        cartTitleLabel = new Label();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        headerPanel.SuspendLayout();
        headerRightPanel.SuspendLayout();
        leftPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)productGrid).BeginInit();
        addPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)quantityNumeric).BeginInit();
        searchPanel.SuspendLayout();
        rightPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cartGrid).BeginInit();
        totalsPanel.SuspendLayout();
        summaryPanel.SuspendLayout();
        summaryTable.SuspendLayout();
        paidPanel.SuspendLayout();
        discountBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)discountValueNumeric).BeginInit();
        cartToolbarPanel.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(31, 58, 95);
        headerPanel.Controls.Add(headerRightPanel);
        headerPanel.Controls.Add(headerTitleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(1200, 64);
        headerPanel.TabIndex = 3;
        //
        // headerRightPanel
        //
        headerRightPanel.AutoSize = true;
        headerRightPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        headerRightPanel.Controls.Add(logoutButton);
        headerRightPanel.Controls.Add(cashierLabel);
        headerRightPanel.Dock = DockStyle.Right;
        headerRightPanel.FlowDirection = FlowDirection.RightToLeft;
        headerRightPanel.Name = "headerRightPanel";
        headerRightPanel.Padding = new Padding(0, 16, 20, 16);
        headerRightPanel.TabIndex = 0;
        headerRightPanel.WrapContents = false;
        //
        // logoutButton
        //
        logoutButton.Margin = new Padding(0);
        logoutButton.Name = "logoutButton";
        logoutButton.Size = new Size(110, 32);
        logoutButton.TabIndex = 0;
        logoutButton.Text = "Sign Out";
        UiTheme.StyleButton(logoutButton, UiTheme.PrimaryLight);
        logoutButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 105, 160);
        logoutButton.Click += LogoutButton_Click;
        //
        // cashierLabel
        //
        cashierLabel.AutoSize = true;
        cashierLabel.Font = new Font("Segoe UI", 9.75F);
        cashierLabel.ForeColor = Color.FromArgb(190, 210, 232);
        cashierLabel.Margin = new Padding(0, 5, 16, 0);
        cashierLabel.Name = "cashierLabel";
        cashierLabel.TabIndex = 1;
        cashierLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // headerTitleLabel
        // 
        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 15F);
        headerTitleLabel.ForeColor = Color.White;
        headerTitleLabel.Location = new Point(20, 17);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Size = new Size(160, 35);
        headerTitleLabel.TabIndex = 2;
        headerTitleLabel.Text = "Point of Sale";
        // 
        // leftPanel
        // 
        leftPanel.Controls.Add(productGrid);
        leftPanel.Controls.Add(addPanel);
        leftPanel.Controls.Add(searchPanel);
        leftPanel.Dock = DockStyle.Left;
        leftPanel.Location = new Point(0, 64);
        leftPanel.Name = "leftPanel";
        leftPanel.Padding = new Padding(12, 12, 6, 12);
        leftPanel.Size = new Size(470, 690);
        leftPanel.TabIndex = 1;
        // 
        // productGrid
        // 
        productGrid.ColumnHeadersHeight = 29;
        productGrid.Dock = DockStyle.Fill;
        productGrid.Location = new Point(12, 74);
        productGrid.Name = "productGrid";
        productGrid.RowHeadersWidth = 51;
        productGrid.Size = new Size(452, 548);
        productGrid.TabIndex = 0;
        productGrid.TabStop = false;
        productGrid.CellDoubleClick += ProductGrid_CellDoubleClick;
        // 
        // addPanel
        // 
        addPanel.Controls.Add(addToCartButton);
        addPanel.Controls.Add(quantityNumeric);
        addPanel.Controls.Add(quantityLabel);
        addPanel.Dock = DockStyle.Bottom;
        addPanel.Location = new Point(12, 622);
        addPanel.Name = "addPanel";
        addPanel.Padding = new Padding(0, 10, 0, 0);
        addPanel.Size = new Size(452, 56);
        addPanel.TabIndex = 1;
        // 
        // addToCartButton
        // 
        addToCartButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        addToCartButton.Location = new Point(118, 14);
        addToCartButton.Name = "addToCartButton";
        addToCartButton.Size = new Size(586, 34);
        addToCartButton.TabIndex = 0;
        addToCartButton.Text = "Add to Cart";
        addToCartButton.Click += AddToCartButton_Click;
        // 
        // quantityNumeric
        // 
        quantityNumeric.Font = new Font("Segoe UI", 12F);
        quantityNumeric.Location = new Point(38, 16);
        quantityNumeric.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
        quantityNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        quantityNumeric.Name = "quantityNumeric";
        quantityNumeric.Size = new Size(70, 34);
        quantityNumeric.TabIndex = 1;
        quantityNumeric.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // quantityLabel
        // 
        quantityLabel.AutoSize = true;
        quantityLabel.Font = new Font("Segoe UI", 9.75F);
        quantityLabel.Location = new Point(2, 22);
        quantityLabel.Name = "quantityLabel";
        quantityLabel.Size = new Size(37, 23);
        quantityLabel.TabIndex = 2;
        quantityLabel.Text = "Qty";
        // 
        // searchPanel
        // 
        searchPanel.Controls.Add(searchTextBox);
        searchPanel.Controls.Add(searchLabel);
        searchPanel.Dock = DockStyle.Top;
        searchPanel.Location = new Point(12, 12);
        searchPanel.Name = "searchPanel";
        searchPanel.Size = new Size(452, 62);
        searchPanel.TabIndex = 2;
        // 
        // searchTextBox
        // 
        searchTextBox.Dock = DockStyle.Bottom;
        searchTextBox.Font = new Font("Segoe UI", 12F);
        searchTextBox.Location = new Point(0, 28);
        searchTextBox.MaxLength = 150;
        searchTextBox.Name = "searchTextBox";
        searchTextBox.Size = new Size(452, 34);
        searchTextBox.TabIndex = 0;
        searchTextBox.TextChanged += SearchTextBox_TextChanged;
        searchTextBox.KeyDown += SearchTextBox_KeyDown;
        // 
        // searchLabel
        // 
        searchLabel.AutoSize = true;
        searchLabel.Font = new Font("Segoe UI", 9.75F);
        searchLabel.Location = new Point(2, 2);
        searchLabel.Name = "searchLabel";
        searchLabel.Size = new Size(450, 23);
        searchLabel.TabIndex = 1;
        searchLabel.Text = "Scan a code or search by name  (Enter to add exact code)";
        // 
        // rightPanel
        // 
        rightPanel.Controls.Add(cartGrid);
        rightPanel.Controls.Add(totalsPanel);
        rightPanel.Controls.Add(cartToolbarPanel);
        rightPanel.Dock = DockStyle.Fill;
        rightPanel.Location = new Point(470, 64);
        rightPanel.Name = "rightPanel";
        rightPanel.Padding = new Padding(6, 12, 12, 12);
        rightPanel.Size = new Size(730, 690);
        rightPanel.TabIndex = 0;
        // 
        // cartGrid
        // 
        cartGrid.ColumnHeadersHeight = 29;
        cartGrid.Dock = DockStyle.Fill;
        cartGrid.Location = new Point(6, 56);
        cartGrid.Name = "cartGrid";
        cartGrid.RowHeadersWidth = 51;
        cartGrid.Size = new Size(712, 372);
        cartGrid.TabIndex = 0;
        // 
        // totalsPanel
        // 
        totalsPanel.Controls.Add(summaryPanel);
        totalsPanel.Controls.Add(discountBox);
        totalsPanel.Dock = DockStyle.Bottom;
        totalsPanel.Location = new Point(6, 428);
        totalsPanel.Name = "totalsPanel";
        totalsPanel.Padding = new Padding(0, 12, 0, 0);
        totalsPanel.Size = new Size(712, 250);
        totalsPanel.TabIndex = 1;
        // 
        // summaryPanel
        // 
        summaryPanel.Controls.Add(summaryTable);
        summaryPanel.Controls.Add(paidPanel);
        summaryPanel.Controls.Add(checkoutButton);
        summaryPanel.Dock = DockStyle.Fill;
        summaryPanel.Location = new Point(330, 12);
        summaryPanel.Name = "summaryPanel";
        summaryPanel.Padding = new Padding(20, 0, 0, 0);
        summaryPanel.Size = new Size(382, 238);
        summaryPanel.TabIndex = 0;
        // 
        // summaryTable
        // 
        summaryTable.ColumnCount = 2;
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        summaryTable.Controls.Add(subtotalCaptionLabel, 0, 0);
        summaryTable.Controls.Add(subtotalValueLabel, 1, 0);
        summaryTable.Controls.Add(discountCaptionLabel, 0, 1);
        summaryTable.Controls.Add(discountValueLabel2, 1, 1);
        summaryTable.Controls.Add(totalCaptionLabel, 0, 2);
        summaryTable.Controls.Add(totalValueLabel, 1, 2);
        summaryTable.Controls.Add(changeCaptionLabel, 0, 3);
        summaryTable.Controls.Add(changeValueLabel, 1, 3);
        summaryTable.Dock = DockStyle.Fill;
        summaryTable.Location = new Point(20, 0);
        summaryTable.Name = "summaryTable";
        summaryTable.RowCount = 4;
        summaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        summaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        summaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        summaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        summaryTable.Size = new Size(362, 134);
        summaryTable.TabIndex = 0;
        // 
        // subtotalCaptionLabel
        // 
        subtotalCaptionLabel.Location = new Point(3, 0);
        subtotalCaptionLabel.Name = "subtotalCaptionLabel";
        subtotalCaptionLabel.Size = new Size(100, 23);
        subtotalCaptionLabel.TabIndex = 0;
        // 
        // subtotalValueLabel
        // 
        subtotalValueLabel.Location = new Point(202, 0);
        subtotalValueLabel.Name = "subtotalValueLabel";
        subtotalValueLabel.Size = new Size(100, 23);
        subtotalValueLabel.TabIndex = 1;
        // 
        // discountCaptionLabel
        // 
        discountCaptionLabel.Location = new Point(3, 33);
        discountCaptionLabel.Name = "discountCaptionLabel";
        discountCaptionLabel.Size = new Size(100, 23);
        discountCaptionLabel.TabIndex = 2;
        // 
        // discountValueLabel2
        // 
        discountValueLabel2.Location = new Point(202, 33);
        discountValueLabel2.Name = "discountValueLabel2";
        discountValueLabel2.Size = new Size(100, 23);
        discountValueLabel2.TabIndex = 3;
        // 
        // totalCaptionLabel
        // 
        totalCaptionLabel.Font = new Font("Segoe UI Semibold", 13F);
        totalCaptionLabel.Location = new Point(3, 66);
        totalCaptionLabel.Name = "totalCaptionLabel";
        totalCaptionLabel.Size = new Size(100, 23);
        totalCaptionLabel.TabIndex = 4;
        // 
        // totalValueLabel
        // 
        totalValueLabel.ForeColor = Color.FromArgb(31, 58, 95);
        totalValueLabel.Location = new Point(202, 66);
        totalValueLabel.Name = "totalValueLabel";
        totalValueLabel.Size = new Size(100, 23);
        totalValueLabel.TabIndex = 5;
        // 
        // changeCaptionLabel
        // 
        changeCaptionLabel.Font = new Font("Segoe UI Semibold", 11F);
        changeCaptionLabel.Location = new Point(3, 99);
        changeCaptionLabel.Name = "changeCaptionLabel";
        changeCaptionLabel.Size = new Size(100, 23);
        changeCaptionLabel.TabIndex = 6;
        // 
        // changeValueLabel
        // 
        changeValueLabel.ForeColor = Color.FromArgb(24, 128, 76);
        changeValueLabel.Location = new Point(202, 99);
        changeValueLabel.Name = "changeValueLabel";
        changeValueLabel.Size = new Size(100, 23);
        changeValueLabel.TabIndex = 7;
        // 
        // paidPanel
        // 
        paidPanel.Controls.Add(amountPaidTextBox);
        paidPanel.Controls.Add(paidLabel);
        paidPanel.Dock = DockStyle.Bottom;
        paidPanel.Location = new Point(20, 134);
        paidPanel.Name = "paidPanel";
        paidPanel.Size = new Size(362, 46);
        paidPanel.TabIndex = 1;
        // 
        // amountPaidTextBox
        // 
        amountPaidTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        amountPaidTextBox.Font = new Font("Segoe UI", 14F);
        amountPaidTextBox.Location = new Point(130, 6);
        amountPaidTextBox.Name = "amountPaidTextBox";
        amountPaidTextBox.Size = new Size(148, 39);
        amountPaidTextBox.TabIndex = 0;
        amountPaidTextBox.Text = "0.00";
        amountPaidTextBox.TextAlign = HorizontalAlignment.Right;
        amountPaidTextBox.TextChanged += AmountPaidTextBox_TextChanged;
        // 
        // paidLabel
        // 
        paidLabel.AutoSize = true;
        paidLabel.Font = new Font("Segoe UI Semibold", 11F);
        paidLabel.Location = new Point(2, 12);
        paidLabel.Name = "paidLabel";
        paidLabel.Size = new Size(123, 25);
        paidLabel.TabIndex = 1;
        paidLabel.Text = "Amount Paid";
        // 
        // checkoutButton
        // 
        checkoutButton.Dock = DockStyle.Bottom;
        checkoutButton.Font = new Font("Segoe UI Semibold", 13F);
        checkoutButton.Location = new Point(20, 180);
        checkoutButton.Name = "checkoutButton";
        checkoutButton.Size = new Size(362, 58);
        checkoutButton.TabIndex = 2;
        checkoutButton.Text = "COMPLETE SALE  (F9)";
        checkoutButton.Click += CheckoutButton_Click;
        // 
        // discountBox
        // 
        discountBox.Controls.Add(discountNoteLabel);
        discountBox.Controls.Add(applyDiscountButton);
        discountBox.Controls.Add(discountValueNumeric);
        discountBox.Controls.Add(discountValueLabel);
        discountBox.Controls.Add(discountTypeCombo);
        discountBox.Controls.Add(discountTypeLabel);
        discountBox.Dock = DockStyle.Left;
        discountBox.Font = new Font("Segoe UI", 9.75F);
        discountBox.Location = new Point(0, 12);
        discountBox.Name = "discountBox";
        discountBox.Padding = new Padding(10);
        discountBox.Size = new Size(330, 238);
        discountBox.TabIndex = 1;
        discountBox.TabStop = false;
        discountBox.Text = "Discount";
        // 
        // discountNoteLabel
        // 
        discountNoteLabel.ForeColor = Color.FromArgb(105, 115, 130);
        discountNoteLabel.Location = new Point(14, 148);
        discountNoteLabel.Name = "discountNoteLabel";
        discountNoteLabel.Size = new Size(300, 44);
        discountNoteLabel.TabIndex = 0;
        discountNoteLabel.Text = "Currently applied: No discount";
        // 
        // applyDiscountButton
        // 
        applyDiscountButton.Location = new Point(80, 106);
        applyDiscountButton.Name = "applyDiscountButton";
        applyDiscountButton.Size = new Size(220, 32);
        applyDiscountButton.TabIndex = 1;
        applyDiscountButton.Text = "Apply Discount";
        applyDiscountButton.Click += ApplyDiscountButton_Click;
        // 
        // discountValueNumeric
        // 
        discountValueNumeric.DecimalPlaces = 2;
        discountValueNumeric.Enabled = false;
        discountValueNumeric.Font = new Font("Segoe UI", 9.75F);
        discountValueNumeric.Location = new Point(80, 68);
        discountValueNumeric.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        discountValueNumeric.Name = "discountValueNumeric";
        discountValueNumeric.Size = new Size(220, 29);
        discountValueNumeric.TabIndex = 2;
        // 
        // discountValueLabel
        // 
        discountValueLabel.AutoSize = true;
        discountValueLabel.Location = new Point(14, 72);
        discountValueLabel.Name = "discountValueLabel";
        discountValueLabel.Size = new Size(52, 23);
        discountValueLabel.TabIndex = 3;
        discountValueLabel.Text = "Value";
        // 
        // discountTypeCombo
        // 
        discountTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        discountTypeCombo.Font = new Font("Segoe UI", 9.75F);
        discountTypeCombo.Items.AddRange(new object[] { "No discount", "Percentage (%)", "Flat amount" });
        discountTypeCombo.Location = new Point(80, 28);
        discountTypeCombo.Name = "discountTypeCombo";
        discountTypeCombo.Size = new Size(220, 29);
        discountTypeCombo.TabIndex = 4;
        discountTypeCombo.SelectedIndexChanged += DiscountTypeCombo_SelectedIndexChanged;
        // 
        // discountTypeLabel
        // 
        discountTypeLabel.AutoSize = true;
        discountTypeLabel.Location = new Point(14, 32);
        discountTypeLabel.Name = "discountTypeLabel";
        discountTypeLabel.Size = new Size(45, 23);
        discountTypeLabel.TabIndex = 5;
        discountTypeLabel.Text = "Type";
        // 
        // cartToolbarPanel
        // 
        cartToolbarPanel.Controls.Add(clearCartButton);
        cartToolbarPanel.Controls.Add(removeLineButton);
        cartToolbarPanel.Controls.Add(changeQuantityButton);
        cartToolbarPanel.Controls.Add(cartTitleLabel);
        cartToolbarPanel.Dock = DockStyle.Top;
        cartToolbarPanel.Location = new Point(6, 12);
        cartToolbarPanel.Name = "cartToolbarPanel";
        cartToolbarPanel.Size = new Size(712, 44);
        cartToolbarPanel.TabIndex = 2;
        // 
        // clearCartButton
        // 
        clearCartButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearCartButton.Location = new Point(1094, 6);
        clearCartButton.Name = "clearCartButton";
        clearCartButton.Size = new Size(110, 30);
        clearCartButton.TabIndex = 0;
        clearCartButton.Text = "Clear Cart";
        clearCartButton.Click += ClearCartButton_Click;
        // 
        // removeLineButton
        // 
        removeLineButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        removeLineButton.Location = new Point(968, 6);
        removeLineButton.Name = "removeLineButton";
        removeLineButton.Size = new Size(120, 30);
        removeLineButton.TabIndex = 1;
        removeLineButton.Text = "Remove Line";
        removeLineButton.Click += RemoveLineButton_Click;
        // 
        // changeQuantityButton
        // 
        changeQuantityButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        changeQuantityButton.Location = new Point(842, 6);
        changeQuantityButton.Name = "changeQuantityButton";
        changeQuantityButton.Size = new Size(120, 30);
        changeQuantityButton.TabIndex = 2;
        changeQuantityButton.Text = "Change Qty";
        changeQuantityButton.Click += ChangeQuantityButton_Click;
        // 
        // cartTitleLabel
        // 
        cartTitleLabel.AutoSize = true;
        cartTitleLabel.Font = new Font("Segoe UI Semibold", 12F);
        cartTitleLabel.Location = new Point(2, 8);
        cartTitleLabel.Name = "cartTitleLabel";
        cartTitleLabel.Size = new Size(123, 28);
        cartTitleLabel.TabIndex = 3;
        cartTitleLabel.Text = "Current Sale";
        // 
        // statusStrip
        // 
        statusStrip.ImageScalingSize = new Size(20, 20);
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 754);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1200, 26);
        statusStrip.TabIndex = 2;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(50, 20);
        statusLabel.Text = "Ready";
        // 
        // PosForm
        // 
        AutoScaleDimensions = new SizeF(9F, 21F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 247, 250);
        ClientSize = new Size(1200, 780);
        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
        Controls.Add(statusStrip);
        Controls.Add(headerPanel);
        Font = new Font("Segoe UI", 9.75F);
        KeyPreview = true;
        MinimumSize = new Size(1100, 720);
        Name = "PosForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Point of Sale — Mart Management System";
        WindowState = FormWindowState.Maximized;
        Load += PosForm_Load;
        KeyDown += PosForm_KeyDown;
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        headerRightPanel.ResumeLayout(false);
        headerRightPanel.PerformLayout();
        leftPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)productGrid).EndInit();
        addPanel.ResumeLayout(false);
        addPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)quantityNumeric).EndInit();
        searchPanel.ResumeLayout(false);
        searchPanel.PerformLayout();
        rightPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)cartGrid).EndInit();
        totalsPanel.ResumeLayout(false);
        summaryPanel.ResumeLayout(false);
        summaryTable.ResumeLayout(false);
        paidPanel.ResumeLayout(false);
        paidPanel.PerformLayout();
        discountBox.ResumeLayout(false);
        discountBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)discountValueNumeric).EndInit();
        cartToolbarPanel.ResumeLayout(false);
        cartToolbarPanel.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private static void ConfigureSummaryCaption(Label label, string text)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Font = UiTheme.BodyFont;
        label.Text = text;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureSummaryValue(Label label, Font font)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.Font = font;
        label.Text = "0.00";
        label.TextAlign = ContentAlignment.MiddleRight;
    }

    private static DataGridViewTextBoxColumn MakeColumn(
        string propertyName,
        string header,
        int width,
        DataGridViewAutoSizeColumnMode sizeMode) =>
        new()
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            Name = "col" + propertyName,
            Width = width,
            AutoSizeMode = sizeMode,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

    private static DataGridViewTextBoxColumn MakeMoneyColumn(string propertyName, string header, int width)
    {
        var column = MakeColumn(propertyName, header, width, DataGridViewAutoSizeColumnMode.None);
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        column.DefaultCellStyle.Format = "N2";
        return column;
    }
}

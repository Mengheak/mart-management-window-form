using MiniMart.Presentation.Common;

namespace MiniMart.Presentation.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel headerPanel;
    private Label titleLabel;
    private Label subtitleLabel;
    private Label usernameLabel;
    private TextBox usernameTextBox;
    private Label passwordLabel;
    private TextBox passwordTextBox;
    private Button signInButton;
    private Button exitButton;
    private Label messageLabel;
    private Label databaseLabel;

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
        subtitleLabel = new Label();
        titleLabel = new Label();
        usernameLabel = new Label();
        usernameTextBox = new TextBox();
        passwordLabel = new Label();
        passwordTextBox = new TextBox();
        signInButton = new Button();
        exitButton = new Button();
        messageLabel = new Label();
        databaseLabel = new Label();
        headerPanel.SuspendLayout();
        SuspendLayout();
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(31, 58, 95);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(380, 110);
        headerPanel.TabIndex = 7;
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Segoe UI", 9.75F);
        subtitleLabel.ForeColor = Color.FromArgb(178, 200, 226);
        subtitleLabel.Location = new Point(34, 66);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(301, 23);
        subtitleLabel.TabIndex = 0;
        subtitleLabel.Text = "Management System — please sign in";
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI Semibold", 19F);
        titleLabel.ForeColor = Color.White;
        titleLabel.Location = new Point(32, 26);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(166, 45);
        titleLabel.TabIndex = 1;
        titleLabel.Text = "Mart";
        // 
        // usernameLabel
        // 
        usernameLabel.AutoSize = true;
        usernameLabel.Font = new Font("Segoe UI", 9.75F);
        usernameLabel.Location = new Point(32, 140);
        usernameLabel.Name = "usernameLabel";
        usernameLabel.Size = new Size(87, 23);
        usernameLabel.TabIndex = 6;
        usernameLabel.Text = "Username";
        // 
        // usernameTextBox
        // 
        usernameTextBox.Font = new Font("Segoe UI", 11F);
        usernameTextBox.Location = new Point(34, 167);
        usernameTextBox.MaxLength = 50;
        usernameTextBox.Name = "usernameTextBox";
        usernameTextBox.Size = new Size(316, 32);
        usernameTextBox.TabIndex = 0;
        usernameTextBox.TextChanged += usernameTextBox_TextChanged;
        // 
        // passwordLabel
        // 
        passwordLabel.AutoSize = true;
        passwordLabel.Font = new Font("Segoe UI", 9.75F);
        passwordLabel.Location = new Point(32, 202);
        passwordLabel.Name = "passwordLabel";
        passwordLabel.Size = new Size(80, 23);
        passwordLabel.TabIndex = 5;
        passwordLabel.Text = "Password";
        // 
        // passwordTextBox
        // 
        passwordTextBox.Font = new Font("Segoe UI", 11F);
        passwordTextBox.Location = new Point(32, 224);
        passwordTextBox.MaxLength = 128;
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(316, 32);
        passwordTextBox.TabIndex = 1;
        passwordTextBox.UseSystemPasswordChar = true;
        // 
        // signInButton
        // 
        signInButton.Font = new Font("Segoe UI Semibold", 11F);
        signInButton.Location = new Point(32, 298);
        signInButton.Name = "signInButton";
        signInButton.Size = new Size(316, 42);
        signInButton.TabIndex = 2;
        signInButton.Text = "Sign In";
        signInButton.Click += SignInButton_Click;
        // 
        // exitButton
        // 
        exitButton.Location = new Point(32, 348);
        exitButton.Name = "exitButton";
        exitButton.Size = new Size(316, 32);
        exitButton.TabIndex = 3;
        exitButton.Text = "Exit";
        exitButton.Click += ExitButton_Click;
        // 
        // messageLabel
        // 
        messageLabel.Font = new Font("Segoe UI", 9.75F);
        messageLabel.ForeColor = Color.FromArgb(178, 46, 46);
        messageLabel.Location = new Point(32, 258);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(316, 34);
        messageLabel.TabIndex = 4;
        // 
        // databaseLabel
        // 
        databaseLabel.Dock = DockStyle.Bottom;
        databaseLabel.Font = new Font("Segoe UI", 8F);
        databaseLabel.ForeColor = Color.FromArgb(105, 115, 130);
        databaseLabel.Location = new Point(0, 396);
        databaseLabel.Name = "databaseLabel";
        databaseLabel.Size = new Size(380, 24);
        databaseLabel.TabIndex = 8;
        databaseLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // LoginForm
        // 
        AcceptButton = signInButton;
        AutoScaleDimensions = new SizeF(9F, 21F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(380, 420);
        Controls.Add(exitButton);
        Controls.Add(signInButton);
        Controls.Add(messageLabel);
        Controls.Add(passwordTextBox);
        Controls.Add(passwordLabel);
        Controls.Add(usernameTextBox);
        Controls.Add(usernameLabel);
        Controls.Add(headerPanel);
        Controls.Add(databaseLabel);
        Font = new Font("Segoe UI", 9.75F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sign In — Mart Management System";
        Load += LoginForm_Load;
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}

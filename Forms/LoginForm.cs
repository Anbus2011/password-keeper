using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pass.Forms;

public class LoginForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(245, 245, 245);
    private static readonly Color SurfaceColor = Color.White;
    private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
    private static readonly Color LabelColor = Color.FromArgb(100, 100, 100);
    private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
    private static readonly Color AccentHover = Color.FromArgb(0, 100, 180);
    private static readonly Color BorderColor = Color.FromArgb(218, 220, 224);

    private static readonly Font MainFont = new("Segoe UI", 10f);
    private static readonly Font LabelFont = new("Segoe UI", 9f);
    private static readonly Font ButtonFont = new("Segoe UI Semibold", 9.5f);
    private static readonly Font TitleFont = new("Segoe UI Semibold", 13f);

    private TextBox _passwordBox = null!;
    private TextBox? _confirmBox;
    private Button _unlockButton = null!;
    private Label _errorLabel = null!;
    private readonly bool _isNewVault;

    public string MasterPassword { get; private set; } = "";

    public LoginForm(bool isNewVault)
    {
        _isNewVault = isNewVault;
        InitializeUI();
    }

    private static Icon CreateKeyIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(218, 165, 32), 2.2f);
        g.DrawEllipse(pen, 2, 1, 12, 12);
        g.DrawEllipse(pen, 5, 4, 6, 6);
        g.DrawLine(pen, 12, 11, 27, 26);
        g.DrawLine(pen, 20, 19, 16, 23);
        g.DrawLine(pen, 23, 22, 19, 26);
        g.DrawLine(pen, 27, 26, 23, 30);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void InitializeUI()
    {
        Text = _isNewVault ? "Create New Vault" : "Unlock Vault";
        Size = new Size(400, _isNewVault ? 320 : 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        Font = MainFont;
        Icon = CreateKeyIcon();

        // Card panel
        var card = new Panel
        {
            Location = new Point(20, 16),
            Size = new Size(Width - 56, Height - 80),
            BackColor = SurfaceColor
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        Controls.Add(card);

        int y = 16;

        var title = new Label
        {
            Text = _isNewVault ? "Create Master Password" : "Enter Master Password",
            Location = new Point(20, y),
            AutoSize = true,
            Font = TitleFont,
            ForeColor = TextColor
        };
        card.Controls.Add(title);
        y += 36;

        var label = new Label
        {
            Text = _isNewVault ? "Choose a strong password:" : "Password:",
            Location = new Point(20, y),
            AutoSize = true,
            Font = LabelFont,
            ForeColor = LabelColor
        };
        card.Controls.Add(label);
        y += 22;

        _passwordBox = new TextBox
        {
            Location = new Point(20, y),
            Size = new Size(card.Width - 40, 28),
            UseSystemPasswordChar = true,
            Font = MainFont,
            BackColor = Color.White,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.Controls.Add(_passwordBox);
        y += 36;

        if (_isNewVault)
        {
            var confirmLabel = new Label
            {
                Text = "Confirm password:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = LabelFont,
                ForeColor = LabelColor
            };
            card.Controls.Add(confirmLabel);
            y += 22;

            _confirmBox = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(card.Width - 40, 28),
                UseSystemPasswordChar = true,
                Font = MainFont,
                BackColor = Color.White,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(_confirmBox);
            y += 36;
        }

        _unlockButton = new Button
        {
            Text = _isNewVault ? "Create Vault" : "Unlock",
            Location = new Point(20, y),
            Size = new Size(card.Width - 40, 34),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = AccentColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = AccentHover
            }
        };
        _unlockButton.Click += OnUnlockClick;
        card.Controls.Add(_unlockButton);
        AcceptButton = _unlockButton;

        _errorLabel = new Label
        {
            Location = new Point(20, y + 40),
            AutoSize = true,
            ForeColor = Color.FromArgb(210, 60, 60),
            Font = LabelFont
        };
        card.Controls.Add(_errorLabel);
    }

    private void OnUnlockClick(object? sender, EventArgs e)
    {
        var pw = _passwordBox.Text;

        if (string.IsNullOrEmpty(pw))
        {
            _errorLabel.Text = "Password cannot be empty.";
            return;
        }

        if (_isNewVault)
        {
            if (pw != _confirmBox!.Text)
            {
                _errorLabel.Text = "Passwords do not match.";
                return;
            }
            if (pw.Length < 4)
            {
                _errorLabel.Text = "Password must be at least 4 characters.";
                return;
            }
        }

        MasterPassword = pw;
        DialogResult = DialogResult.OK;
        Close();
    }
}

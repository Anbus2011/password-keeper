using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Pass.Services;

namespace Pass.Forms;

public class WrongPasswordForm : Form
{
    private static readonly Font TitleFont = new("Segoe UI Semibold", 12f);
    private static readonly Font BodyFont = new("Segoe UI", 9.5f);
    private static readonly Font ButtonFont = new("Segoe UI Semibold", 9f);

    public WrongPasswordForm()
    {
        Text = "Wrong Password";
        Size = new Size(360, 210);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.BgColor;

        var card = new Panel
        {
            Location = new Point(16, 12),
            Size = new Size(Width - 48, Height - 64),
            BackColor = Theme.SurfaceColor
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        Controls.Add(card);

        var title = new Label
        {
            Text = "Wrong Password",
            Location = new Point(20, 16),
            AutoSize = true,
            Font = TitleFont,
            ForeColor = Theme.DangerColor
        };
        card.Controls.Add(title);

        var body = new Label
        {
            Text = "The password you entered is incorrect.\nPress OK to try again, or Cancel to exit.",
            Location = new Point(20, 48),
            Size = new Size(card.Width - 40, 40),
            Font = BodyFont,
            ForeColor = Theme.LabelColor
        };
        card.Controls.Add(body);

        var okButton = new Button
        {
            Text = "OK",
            Location = new Point(card.Width - 190, 94),
            Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = Theme.AccentColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Theme.AccentHover }
        };
        okButton.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        card.Controls.Add(okButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(card.Width - 100, 94),
            Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = Theme.DangerColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = Theme.DangerHover
            }
        };
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        card.Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}

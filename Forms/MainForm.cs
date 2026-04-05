using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using Pass.Models;
using Pass.Services;

namespace Pass.Forms;

public class MainForm : Form
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int EM_SETMARGINS = 0xD3;
    private const int EC_LEFTMARGIN = 0x1;

    // Modern light theme colors
    private static readonly Color BgColor = Color.FromArgb(245, 245, 245);
    private static readonly Color SurfaceColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(218, 220, 224);
    private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
    private static readonly Color LabelColor = Color.FromArgb(100, 100, 100);
    private static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
    private static readonly Color AccentHover = Color.FromArgb(0, 100, 180);
    private static readonly Color DangerColor = Color.FromArgb(210, 60, 60);
    private static readonly Color ListSelectColor = Color.FromArgb(230, 240, 255);
    private static readonly Color MenuBg = Color.White;
    private static readonly Color GoldColor = Color.FromArgb(218, 165, 32);
    private static readonly Color HeaderBg = Color.FromArgb(0, 120, 212);

    private static readonly Font MainFont = new("Segoe UI", 9.5f);
    private static readonly Font LabelFont = new("Segoe UI", 8.5f);
    private static readonly Font SearchFont = new("Segoe UI", 10f);
    private static readonly Font ListFont = new("Segoe UI", 9.5f);
    private static readonly Font ButtonFont = new("Segoe UI Semibold", 9f);
    private static readonly Font HeaderFont = new("Segoe UI Semibold", 12f);
    private static readonly Font HeaderFileFont = new("Segoe UI", 9f);

    private VaultService _vault;
    private List<VaultEntry> _filtered = new();
    private VaultEntry? _current;
    private bool _isNew;

    // Left pane
    private TextBox _searchBox = null!;
    private ListBox _entryList = null!;

    // Right pane
    private TextBox _titleBox = null!;
    private TextBox _usernameBox = null!;
    private TextBox _passwordBox = null!;
    private CheckBox _showPassword = null!;
    private TextBox _urlBox = null!;
    private TextBox _notesBox = null!;
    private Button _newButton = null!;
    private Button _saveButton = null!;
    private Button _deleteButton = null!;
    private Button _generateButton = null!;
    private Button _copyUserButton = null!;
    private Button _copyPassButton = null!;
    private Button _copyUrlButton = null!;

    public MainForm(VaultService vault)
    {
        _vault = vault;
        _filtered = new List<VaultEntry>(vault.Entries);
        Icon = CreateKeyIcon();
        InitializeUI();
        RefreshList();
        SetNewMode();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var path = _vault.VaultPath;
        if (string.IsNullOrEmpty(path))
        {
            Text = "Pass";
        }
        else
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            Text = $"Pass: {name}";
        }
    }

    private static Icon CreateKeyIcon()
    {
        var bmp = new Bitmap(48, 48);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var pen = new Pen(GoldColor, 3f);

        // Key head (ring)
        g.DrawEllipse(pen, 3, 2, 18, 18);
        g.DrawEllipse(pen, 7, 6, 10, 10);

        // Shaft
        g.DrawLine(pen, 18, 16, 40, 38);

        // Teeth
        g.DrawLine(pen, 30, 28, 24, 34);
        g.DrawLine(pen, 34, 32, 28, 38);
        g.DrawLine(pen, 40, 38, 34, 44);

        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void InitializeUI()
    {
        Size = new Size(860, 540);
        MinimumSize = new Size(720, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        Font = MainFont;

        // Menu bar
        var menuStrip = new MenuStrip
        {
            BackColor = MenuBg,
            ForeColor = TextColor,
            Font = MainFont,
            Padding = new Padding(6, 2, 0, 2)
        };
        menuStrip.Renderer = new ModernMenuRenderer();

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("&New Vault...", null, OnMenuNew);
        fileMenu.DropDownItems.Add("&Open Vault...", null, OnMenuOpen);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("&Save", null, OnMenuSave);
        fileMenu.DropDownItems.Add("Export As &Text...", null, OnMenuExportText);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("&Close", null, (_, _) => Close());

        ((ToolStripMenuItem)fileMenu.DropDownItems[0]).ShortcutKeys = Keys.Control | Keys.N;
        ((ToolStripMenuItem)fileMenu.DropDownItems[1]).ShortcutKeys = Keys.Control | Keys.O;
        ((ToolStripMenuItem)fileMenu.DropDownItems[3]).ShortcutKeys = Keys.Control | Keys.S;
        ((ToolStripMenuItem)fileMenu.DropDownItems[4]).ShortcutKeys = Keys.Control | Keys.E;

        menuStrip.Items.Add(fileMenu);
        MainMenuStrip = menuStrip;

        // Main content panel with padding
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 12),
            BackColor = BgColor
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 240,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 8,
            BackColor = BgColor
        };
        split.Panel1.BackColor = BgColor;
        split.Panel2.BackColor = BgColor;

        contentPanel.Controls.Add(split);
        Controls.Add(contentPanel);
        Controls.Add(menuStrip);

        BuildLeftPane(split.Panel1);
        BuildRightPane(split.Panel2);
    }

    private void BuildLeftPane(SplitterPanel panel)
    {
        // Search box with padding wrapper
        var searchPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(0, 0, 0, 6)
        };

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "\U0001F50D  Search...",
            Font = SearchFont,
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        _searchBox.TextChanged += (_, _) => ApplyFilter();
        searchPanel.Controls.Add(_searchBox);

        _entryList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = ListFont,
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30
        };
        _entryList.DrawItem += OnDrawListItem;
        _entryList.SelectedIndexChanged += OnEntrySelected;

        panel.Controls.Add(_entryList);
        panel.Controls.Add(searchPanel);
    }

    private void OnDrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var isSelected = (e.State & DrawItemState.Selected) != 0;
        var bg = isSelected ? ListSelectColor : SurfaceColor;
        var fg = TextColor;

        using var bgBrush = new SolidBrush(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        var text = _entryList.Items[e.Index].ToString() ?? "";
        var textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, ListFont, textBounds, fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        if (isSelected)
        {
            using var accentPen = new Pen(AccentColor, 3);
            e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom);
        }
    }

    private void BuildRightPane(SplitterPanel panel)
    {
        // White card surface
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceColor,
            Padding = new Padding(20, 16, 20, 16)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        panel.Controls.Add(card);

        var inner = new Panel { Dock = DockStyle.Fill };
        card.Controls.Add(inner);

        int y = 0;
        const int labelW = 80;
        const int copyW = 54;
        const int rowH = 30;
        const int gap = 12;
        int tabIdx = 0;

        // Title
        inner.Controls.Add(MakeLabel("Title", y));
        _titleBox = MakeTextBox(labelW, y, inner.Width - labelW - 8);
        _titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _titleBox.TabIndex = tabIdx++;
        inner.Controls.Add(_titleBox);
        y += rowH + gap;

        // Username
        inner.Controls.Add(MakeLabel("Username", y));
        _usernameBox = MakeTextBox(labelW, y, inner.Width - labelW - copyW - 16);
        _usernameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _usernameBox.TabIndex = tabIdx++;
        inner.Controls.Add(_usernameBox);
        _copyUserButton = MakeSmallButton("Copy", inner.Width - copyW - 4, y, copyW);
        _copyUserButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyUserButton.TabStop = false;
        _copyUserButton.Click += (_, _) => CopyToClipboard(_usernameBox.Text);
        inner.Controls.Add(_copyUserButton);
        y += rowH + gap;

        // Password
        inner.Controls.Add(MakeLabel("Password", y));
        _passwordBox = MakeTextBox(labelW, y, inner.Width - labelW - copyW - 76);
        _passwordBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _passwordBox.TabIndex = tabIdx++;
        inner.Controls.Add(_passwordBox);

        _showPassword = new CheckBox
        {
            Text = "Show",
            Location = new Point(inner.Width - copyW - 66, y + 2),
            Size = new Size(58, rowH - 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Checked = true,
            TabStop = false,
            Font = LabelFont,
            ForeColor = LabelColor,
            FlatStyle = FlatStyle.Flat
        };
        _showPassword.CheckedChanged += (_, _) =>
            _passwordBox.UseSystemPasswordChar = !_showPassword.Checked;
        inner.Controls.Add(_showPassword);

        _copyPassButton = MakeSmallButton("Copy", inner.Width - copyW - 4, y, copyW);
        _copyPassButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyPassButton.TabStop = false;
        _copyPassButton.Click += (_, _) => CopyToClipboard(_passwordBox.Text);
        inner.Controls.Add(_copyPassButton);
        y += rowH + gap;

        // Generate password
        _generateButton = MakeAccentButton("Generate Password", labelW, y, 150);
        _generateButton.TabStop = false;
        _generateButton.Click += OnGenerateClick;
        inner.Controls.Add(_generateButton);
        y += rowH + gap + 2;

        // URL
        inner.Controls.Add(MakeLabel("URL", y));
        _urlBox = MakeTextBox(labelW, y, inner.Width - labelW - copyW - 16);
        _urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _urlBox.TabIndex = tabIdx++;
        inner.Controls.Add(_urlBox);
        _copyUrlButton = MakeSmallButton("Copy", inner.Width - copyW - 4, y, copyW);
        _copyUrlButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyUrlButton.TabStop = false;
        _copyUrlButton.Click += (_, _) => CopyToClipboard(_urlBox.Text);
        inner.Controls.Add(_copyUrlButton);
        y += rowH + gap;

        // Notes
        inner.Controls.Add(MakeLabel("Notes", y));
        _notesBox = new TextBox
        {
            Location = new Point(labelW, y),
            Size = new Size(inner.Width - labelW - 8, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            TabIndex = tabIdx++,
            AcceptsTab = false,
            Font = MainFont,
            BackColor = SurfaceColor,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        inner.Controls.Add(_notesBox);

        // Button row
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = SurfaceColor
        };

        _saveButton = MakeAccentButton("Save", 0, 0, 90);
        _saveButton.TabIndex = tabIdx++;
        _saveButton.Click += OnSaveClick;

        _newButton = MakeOutlineButton("New", 0, 0, 90);
        _newButton.TabIndex = tabIdx++;
        _newButton.Click += OnNewClick;

        _deleteButton = MakeDangerButton("Delete", 0, 0, 90);
        _deleteButton.TabIndex = tabIdx++;
        _deleteButton.Click += OnDeleteClick;

        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(_newButton);
        buttonPanel.Controls.Add(_deleteButton);

        inner.Controls.Add(buttonPanel);
    }

    // --- Styled control factories ---

    private static Label MakeLabel(string text, int y) =>
        new()
        {
            Text = text,
            Location = new Point(0, y + 5),
            AutoSize = true,
            Font = LabelFont,
            ForeColor = LabelColor
        };

    private static TextBox MakeTextBox(int x, int y, int width)
    {
        var tb = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 26),
            Font = MainFont,
            BackColor = Color.White,
            ForeColor = TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        tb.HandleCreated += (_, _) =>
            SendMessage(tb.Handle, EM_SETMARGINS, (IntPtr)EC_LEFTMARGIN, (IntPtr)2);
        return tb;
    }

    private static Button MakeSmallButton(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 26),
            FlatStyle = FlatStyle.Flat,
            Font = LabelFont,
            BackColor = BgColor,
            ForeColor = TextColor,
            FlatAppearance =
            {
                BorderColor = BorderColor,
                BorderSize = 1,
                MouseOverBackColor = Color.FromArgb(230, 230, 230)
            },
            Cursor = Cursors.Hand
        };

    private static Button MakeAccentButton(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = AccentHover
            },
            Cursor = Cursors.Hand
        };

    private static Button MakeOutlineButton(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = SurfaceColor,
            ForeColor = AccentColor,
            FlatAppearance =
            {
                BorderColor = AccentColor,
                BorderSize = 1,
                MouseOverBackColor = Color.FromArgb(230, 240, 255)
            },
            Cursor = Cursors.Hand
        };

    private static Button MakeDangerButton(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = SurfaceColor,
            ForeColor = DangerColor,
            FlatAppearance =
            {
                BorderColor = DangerColor,
                BorderSize = 1,
                MouseOverBackColor = Color.FromArgb(255, 235, 235)
            },
            Cursor = Cursors.Hand
        };

    private static void CopyToClipboard(string text)
    {
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    // --- State management ---

    private void SetNewMode()
    {
        _isNew = true;
        _current = new VaultEntry();
        _saveButton.Text = "Add";
        _deleteButton.Enabled = false;
    }

    private void SetEditMode()
    {
        _isNew = false;
        _saveButton.Text = "Save";
        _deleteButton.Enabled = true;
    }

    private void SetVaultEnabled(bool enabled)
    {
        _searchBox.Enabled = enabled;
        _entryList.Enabled = enabled;
        _titleBox.Enabled = enabled;
        _usernameBox.Enabled = enabled;
        _passwordBox.Enabled = enabled;
        _urlBox.Enabled = enabled;
        _notesBox.Enabled = enabled;
        _newButton.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _deleteButton.Enabled = enabled;
        _generateButton.Enabled = enabled;
        _showPassword.Enabled = enabled;
    }

    private void ReloadVault()
    {
        _filtered = new List<VaultEntry>(_vault.Entries);
        _searchBox.Text = "";
        ClearFields();
        RefreshList();
        SetNewMode();
        SetVaultEnabled(_vault.IsOpen);
        UpdateTitle();
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        _filtered = string.IsNullOrEmpty(q)
            ? new List<VaultEntry>(_vault.Entries)
            : _vault.Entries.Where(e =>
                e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Url.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        RefreshList();
    }

    private void RefreshList()
    {
        _entryList.BeginUpdate();
        _entryList.Items.Clear();
        foreach (var entry in _filtered)
            _entryList.Items.Add(entry.Title);
        _entryList.EndUpdate();
    }

    private void OnEntrySelected(object? sender, EventArgs e)
    {
        if (_entryList.SelectedIndex < 0 || _entryList.SelectedIndex >= _filtered.Count)
            return;

        _current = _filtered[_entryList.SelectedIndex];
        LoadFields(_current);
        SetEditMode();
    }

    private void LoadFields(VaultEntry entry)
    {
        _titleBox.Text = entry.Title;
        _usernameBox.Text = entry.Username;
        _passwordBox.Text = entry.Password;
        _urlBox.Text = entry.Url;
        _notesBox.Text = entry.Notes;
    }

    private void ClearFields()
    {
        _titleBox.Text = "";
        _usernameBox.Text = "";
        _passwordBox.Text = "";
        _urlBox.Text = "";
        _notesBox.Text = "";
    }

    private void OnNewClick(object? sender, EventArgs e)
    {
        _entryList.ClearSelected();
        ClearFields();
        SetNewMode();
        _titleBox.Focus();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            MessageBox.Show("Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_current == null)
        {
            _current = new VaultEntry();
            _isNew = true;
        }

        _current.Title = _titleBox.Text.Trim();
        _current.Username = _usernameBox.Text;
        _current.Password = _passwordBox.Text;
        _current.Url = _urlBox.Text;
        _current.Notes = _notesBox.Text;
        _current.Modified = DateTime.UtcNow;

        if (_isNew)
        {
            _current.Created = DateTime.UtcNow;
            _vault.Entries.Add(_current);
        }

        _vault.Save();
        ApplyFilter();
        SetEditMode();

        var idx = _filtered.IndexOf(_current);
        if (idx >= 0)
            _entryList.SelectedIndex = idx;
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_current == null || _isNew) return;

        var result = MessageBox.Show(
            $"Delete \"{_current.Title}\"?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        _vault.Entries.Remove(_current);
        _vault.Save();
        ClearFields();
        ApplyFilter();
        SetNewMode();
    }

    private void OnGenerateClick(object? sender, EventArgs e)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*-_=+";
        var buf = new byte[20];
        System.Security.Cryptography.RandomNumberGenerator.Fill(buf);
        var password = new char[20];
        for (int i = 0; i < 20; i++)
            password[i] = chars[buf[i] % chars.Length];
        _passwordBox.Text = new string(password);
    }

    // --- File menu handlers ---

    private void OnMenuNew(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Create new vault file",
            Filter = "Vault files (*.vlt)|*.vlt",
            DefaultExt = "vlt"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        using var loginForm = new LoginForm(isNewVault: true);
        if (loginForm.ShowDialog() != DialogResult.OK) return;

        try
        {
            _vault.Dispose();
            _vault = new VaultService();
            _vault.Open(sfd.FileName, loginForm.MasterPassword);
            _vault.AcquireLock();
            ConfigService.SaveVaultPath(sfd.FileName);
            ReloadVault();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create vault:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMenuOpen(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Open vault file",
            Filter = "Vault files (*.vlt)|*.vlt|All files (*.*)|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        var lockInfo = VaultService.CheckLock(ofd.FileName);
        if (lockInfo != null)
        {
            var answer = MessageBox.Show(
                $"{lockInfo}\n\nOverride lock and open anyway?",
                "Vault Locked",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        using var loginForm = new LoginForm(isNewVault: false);
        if (loginForm.ShowDialog() != DialogResult.OK) return;

        try
        {
            _vault.Dispose();
            _vault = new VaultService();
            _vault.Open(ofd.FileName, loginForm.MasterPassword);
            _vault.AcquireLock();
            ConfigService.SaveVaultPath(ofd.FileName);
            ReloadVault();
        }
        catch (CryptographicException)
        {
            MessageBox.Show("Wrong password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open vault:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMenuSave(object? sender, EventArgs e)
    {
        if (!_vault.IsOpen)
        {
            MessageBox.Show("No vault is open.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _vault.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnMenuExportText(object? sender, EventArgs e)
    {
        if (!_vault.IsOpen || _vault.Entries.Count == 0)
        {
            MessageBox.Show("No vault is open or the vault is empty.", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Title = "Export vault as plain text",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = System.IO.Path.GetFileNameWithoutExtension(_vault.VaultPath) + "_export"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            var vaultName = System.IO.Path.GetFileNameWithoutExtension(_vault.VaultPath);
            sb.AppendLine($"Pass Vault Export: {vaultName}");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Entries: {_vault.Entries.Count}");
            sb.AppendLine(new string('=', 60));

            for (int i = 0; i < _vault.Entries.Count; i++)
            {
                var entry = _vault.Entries[i];
                sb.AppendLine();
                sb.AppendLine($"[{i + 1}] {entry.Title}");
                sb.AppendLine(new string('-', 40));
                sb.AppendLine($"  Username : {entry.Username}");
                sb.AppendLine($"  Password : {entry.Password}");
                if (!string.IsNullOrWhiteSpace(entry.Url))
                    sb.AppendLine($"  URL      : {entry.Url}");
                if (!string.IsNullOrWhiteSpace(entry.Notes))
                    sb.AppendLine($"  Notes    : {entry.Notes}");
                sb.AppendLine($"  Created  : {entry.Created:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"  Modified : {entry.Modified:yyyy-MM-dd HH:mm}");
            }

            System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
            MessageBox.Show($"Exported {_vault.Entries.Count} entries to:\n{sfd.FileName}",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

// Custom renderer to remove the blue highlight bar on menu items
internal class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color MenuHover = Color.FromArgb(230, 240, 255);

    public ModernMenuRenderer() : base(new ModernColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            using var brush = new SolidBrush(MenuHover);
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
    }
}

internal class ModernColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(218, 220, 224);
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Color.FromArgb(230, 240, 255);
    public override Color MenuStripGradientBegin => Color.White;
    public override Color MenuStripGradientEnd => Color.White;
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color SeparatorDark => Color.FromArgb(230, 230, 230);
    public override Color SeparatorLight => Color.White;
}

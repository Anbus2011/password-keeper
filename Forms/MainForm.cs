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

    private static readonly Font MainFont = new("Segoe UI", 9.5f);
    private static readonly Font LabelFont = new("Segoe UI", 8.5f);
    private static readonly Font SearchFont = new("Segoe UI", 10f);
    private static readonly Font ListFont = new("Segoe UI", 9.5f);
    private static readonly Font ButtonFont = new("Segoe UI Semibold", 9f);

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
    private CheckBox _darkModeCheck = null!;

    // Structural controls for theming
    private MenuStrip _menuStrip = null!;
    private Panel _contentPanel = null!;
    private SplitContainer _split = null!;
    private Panel _card = null!;
    private Panel _innerPanel = null!;
    private FlowLayoutPanel _buttonPanel = null!;
    private Panel _searchPanel = null!;
    private List<Label> _labels = new();

    public MainForm(VaultService vault)
    {
        _vault = vault;
        _filtered = new List<VaultEntry>(vault.Entries);
        Theme.SetDarkMode(ConfigService.LoadDarkMode());
        Icon = CreateKeyIcon();
        InitializeUI();
        RefreshList();
        SetNewMode();
        UpdateTitle();
        FormClosed += (_, _) => _vault.Dispose();
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

        using var pen = new Pen(Theme.GoldColor, 3f);

        g.DrawEllipse(pen, 3, 2, 18, 18);
        g.DrawEllipse(pen, 7, 6, 10, 10);
        g.DrawLine(pen, 18, 16, 40, 38);
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
        BackColor = Theme.BgColor;
        Font = MainFont;

        // Menu bar
        _menuStrip = new MenuStrip
        {
            BackColor = Theme.MenuBg,
            ForeColor = Theme.TextColor,
            Font = MainFont,
            Padding = new Padding(6, 2, 0, 2)
        };
        _menuStrip.Renderer = new ModernMenuRenderer();

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

        _menuStrip.Items.Add(fileMenu);
        MainMenuStrip = _menuStrip;

        // Main content panel with padding
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 12),
            BackColor = Theme.BgColor
        };

        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 240,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 8,
            BackColor = Theme.BgColor
        };
        _split.Panel1.BackColor = Theme.BgColor;
        _split.Panel2.BackColor = Theme.BgColor;

        _contentPanel.Controls.Add(_split);
        Controls.Add(_contentPanel);
        Controls.Add(_menuStrip);

        BuildLeftPane(_split.Panel1);
        BuildRightPane(_split.Panel2);
    }

    private void BuildLeftPane(SplitterPanel panel)
    {
        _searchPanel = new Panel
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
            BackColor = Theme.InputBg,
            ForeColor = Theme.TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        _searchBox.TextChanged += (_, _) => ApplyFilter();
        _searchPanel.Controls.Add(_searchBox);

        _entryList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = ListFont,
            BackColor = Theme.SurfaceColor,
            ForeColor = Theme.TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30
        };
        _entryList.DrawItem += OnDrawListItem;
        _entryList.SelectedIndexChanged += OnEntrySelected;

        panel.Controls.Add(_entryList);
        panel.Controls.Add(_searchPanel);
    }

    private void OnDrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var isSelected = (e.State & DrawItemState.Selected) != 0;
        var bg = isSelected ? Theme.ListSelectColor : Theme.SurfaceColor;
        var fg = Theme.TextColor;

        using var bgBrush = new SolidBrush(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        var text = _entryList.Items[e.Index].ToString() ?? "";
        var textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, ListFont, textBounds, fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        if (isSelected)
        {
            using var accentPen = new Pen(Theme.AccentColor, 3);
            e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom);
        }
    }

    private void BuildRightPane(SplitterPanel panel)
    {
        _card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceColor,
            Padding = new Padding(20, 16, 20, 16)
        };
        _card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, _card.Width - 1, _card.Height - 1);
        };
        panel.Controls.Add(_card);

        _innerPanel = new Panel { Dock = DockStyle.Fill };
        _card.Controls.Add(_innerPanel);

        int y = 0;
        const int labelW = 80;
        const int copyW = 54;
        const int rowH = 30;
        const int gap = 12;
        int tabIdx = 0;

        // Title
        _labels.Add(MakeLabel("Title", y));
        _innerPanel.Controls.Add(_labels[^1]);
        _titleBox = MakeTextBox(labelW, y, _innerPanel.Width - labelW - 8);
        _titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _titleBox.TabIndex = tabIdx++;
        _innerPanel.Controls.Add(_titleBox);
        y += rowH + gap;

        // Username
        _labels.Add(MakeLabel("Username", y));
        _innerPanel.Controls.Add(_labels[^1]);
        _usernameBox = MakeTextBox(labelW, y, _innerPanel.Width - labelW - copyW - 16);
        _usernameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _usernameBox.TabIndex = tabIdx++;
        _innerPanel.Controls.Add(_usernameBox);
        _copyUserButton = MakeSmallButton("Copy", _innerPanel.Width - copyW - 4, y, copyW);
        _copyUserButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyUserButton.TabStop = false;
        _copyUserButton.Click += (_, _) => CopyToClipboard(_usernameBox.Text);
        _innerPanel.Controls.Add(_copyUserButton);
        y += rowH + gap;

        // Password
        _labels.Add(MakeLabel("Password", y));
        _innerPanel.Controls.Add(_labels[^1]);
        _passwordBox = MakeTextBox(labelW, y, _innerPanel.Width - labelW - copyW - 76);
        _passwordBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _passwordBox.TabIndex = tabIdx++;
        _innerPanel.Controls.Add(_passwordBox);

        _showPassword = new CheckBox
        {
            Text = "Show",
            Location = new Point(_innerPanel.Width - copyW - 66, y + 2),
            Size = new Size(58, rowH - 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Checked = true,
            TabStop = false,
            Font = LabelFont,
            ForeColor = Theme.LabelColor
        };
        _showPassword.CheckedChanged += (_, _) =>
            _passwordBox.UseSystemPasswordChar = !_showPassword.Checked;
        _innerPanel.Controls.Add(_showPassword);

        _copyPassButton = MakeSmallButton("Copy", _innerPanel.Width - copyW - 4, y, copyW);
        _copyPassButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyPassButton.TabStop = false;
        _copyPassButton.Click += (_, _) => CopyToClipboard(_passwordBox.Text);
        _innerPanel.Controls.Add(_copyPassButton);
        y += rowH + gap;

        // Generate password
        _generateButton = MakeAccentButton("Generate Password", labelW, y, 150);
        _generateButton.TabStop = false;
        _generateButton.Click += OnGenerateClick;
        _innerPanel.Controls.Add(_generateButton);
        y += rowH + gap + 2;

        // URL
        _labels.Add(MakeLabel("URL", y));
        _innerPanel.Controls.Add(_labels[^1]);
        _urlBox = MakeTextBox(labelW, y, _innerPanel.Width - labelW - copyW - 16);
        _urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _urlBox.TabIndex = tabIdx++;
        _innerPanel.Controls.Add(_urlBox);
        _copyUrlButton = MakeSmallButton("Copy", _innerPanel.Width - copyW - 4, y, copyW);
        _copyUrlButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyUrlButton.TabStop = false;
        _copyUrlButton.Click += (_, _) => CopyToClipboard(_urlBox.Text);
        _innerPanel.Controls.Add(_copyUrlButton);
        y += rowH + gap;

        // Notes
        _labels.Add(MakeLabel("Notes", y));
        _innerPanel.Controls.Add(_labels[^1]);
        _notesBox = new TextBox
        {
            Location = new Point(labelW, y),
            Size = new Size(_innerPanel.Width - labelW - 8, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            TabIndex = tabIdx++,
            AcceptsTab = false,
            Font = MainFont,
            BackColor = Theme.InputBg,
            ForeColor = Theme.TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        _innerPanel.Controls.Add(_notesBox);

        // Button row
        _buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Theme.SurfaceColor
        };

        _saveButton = MakeAccentButton("Save", 0, 0, 90);
        _saveButton.TabIndex = tabIdx++;
        _saveButton.Click += OnSaveClick;

        _newButton = MakeAccentButton("New", 0, 0, 90);
        _newButton.TabIndex = tabIdx++;
        _newButton.Click += OnNewClick;

        _deleteButton = MakeDangerFilledButton("Delete", 0, 0, 90);
        _deleteButton.TabIndex = tabIdx++;
        _deleteButton.Click += OnDeleteClick;

        _darkModeCheck = new CheckBox
        {
            Text = "Dark Mode",
            AutoSize = true,
            Checked = Theme.IsDark,
            Font = LabelFont,
            ForeColor = Theme.LabelColor,
            Margin = new Padding(3, 11, 3, 3),
            TabStop = false
        };
        _darkModeCheck.CheckedChanged += OnDarkModeToggle;

        _buttonPanel.Controls.Add(_saveButton);
        _buttonPanel.Controls.Add(_newButton);
        _buttonPanel.Controls.Add(_deleteButton);

        // Spacer to push dark mode checkbox to the left
        var spacer = new Control { Width = 1, Height = 1 };
        spacer.Dock = DockStyle.None;
        _buttonPanel.Controls.Add(spacer);
        _buttonPanel.SetFlowBreak(spacer, false);

        _innerPanel.Controls.Add(_buttonPanel);

        // Dark mode checkbox in its own left-aligned panel at the bottom
        var darkPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Theme.SurfaceColor
        };
        _darkModeCheck.Location = new Point(0, 8);
        darkPanel.Controls.Add(_darkModeCheck);
        _innerPanel.Controls.Add(darkPanel);
    }

    private void OnDarkModeToggle(object? sender, EventArgs e)
    {
        Theme.SetDarkMode(_darkModeCheck.Checked);
        ConfigService.SaveDarkMode(_darkModeCheck.Checked);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        SuspendLayout();

        // Form
        BackColor = Theme.BgColor;

        // Menu
        _menuStrip.BackColor = Theme.MenuBg;
        _menuStrip.ForeColor = Theme.TextColor;
        _menuStrip.Renderer = new ModernMenuRenderer();
        foreach (ToolStripItem item in _menuStrip.Items)
        {
            item.ForeColor = Theme.TextColor;
            if (item is ToolStripMenuItem mi)
                foreach (ToolStripItem sub in mi.DropDownItems)
                    sub.ForeColor = Theme.TextColor;
        }

        // Content & split
        _contentPanel.BackColor = Theme.BgColor;
        _split.BackColor = Theme.BgColor;
        _split.Panel1.BackColor = Theme.BgColor;
        _split.Panel2.BackColor = Theme.BgColor;

        // Search
        _searchBox.BackColor = Theme.InputBg;
        _searchBox.ForeColor = Theme.TextColor;

        // List
        _entryList.BackColor = Theme.SurfaceColor;
        _entryList.ForeColor = Theme.TextColor;

        // Card
        _card.BackColor = Theme.SurfaceColor;
        _innerPanel.BackColor = Theme.SurfaceColor;

        // Labels
        foreach (var lbl in _labels)
            lbl.ForeColor = Theme.LabelColor;

        // Text boxes
        foreach (var tb in new[] { _titleBox, _usernameBox, _passwordBox, _urlBox, _notesBox })
        {
            tb.BackColor = Theme.InputBg;
            tb.ForeColor = Theme.TextColor;
        }

        // Show password checkbox
        _showPassword.ForeColor = Theme.LabelColor;
        _showPassword.BackColor = Theme.SurfaceColor;

        // Copy buttons
        foreach (var btn in new[] { _copyUserButton, _copyPassButton, _copyUrlButton })
        {
            btn.BackColor = Theme.SmallButtonBg;
            btn.ForeColor = Theme.TextColor;
            btn.FlatAppearance.BorderColor = Theme.BorderColor;
            btn.FlatAppearance.MouseOverBackColor = Theme.SmallButtonHover;
        }

        // Accent buttons
        foreach (var btn in new[] { _saveButton, _generateButton, _newButton })
        {
            btn.BackColor = Theme.AccentColor;
            btn.ForeColor = Color.White;
            btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
        }

        // Danger filled button
        _deleteButton.BackColor = Theme.DangerColor;
        _deleteButton.ForeColor = Color.White;
        _deleteButton.FlatAppearance.MouseOverBackColor = Theme.DangerHover;

        // Button panel & dark mode
        _buttonPanel.BackColor = Theme.SurfaceColor;
        _darkModeCheck.ForeColor = Theme.LabelColor;
        _darkModeCheck.BackColor = Theme.SurfaceColor;
        _darkModeCheck.Parent!.BackColor = Theme.SurfaceColor;

        // Force list repaint
        _entryList.Invalidate();
        _card.Invalidate();

        ResumeLayout(true);
    }

    // --- Styled control factories ---

    private static Label MakeLabel(string text, int y) =>
        new()
        {
            Text = text,
            Location = new Point(0, y + 5),
            AutoSize = true,
            Font = LabelFont,
            ForeColor = Theme.LabelColor
        };

    private static TextBox MakeTextBox(int x, int y, int width)
    {
        var tb = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 26),
            Font = MainFont,
            BackColor = Theme.InputBg,
            ForeColor = Theme.TextColor,
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
            BackColor = Theme.SmallButtonBg,
            ForeColor = Theme.TextColor,
            FlatAppearance =
            {
                BorderColor = Theme.BorderColor,
                BorderSize = 1,
                MouseOverBackColor = Theme.SmallButtonHover
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
            BackColor = Theme.AccentColor,
            ForeColor = Color.White,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = Theme.AccentHover
            },
            Cursor = Cursors.Hand
        };

    private static Button MakeDangerFilledButton(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = Theme.DangerColor,
            ForeColor = Color.White,
            FlatAppearance =
            {
                BorderSize = 0,
                MouseOverBackColor = Theme.DangerHover
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

        using var loginForm = new LoginForm(isNewVault: false, ofd.FileName);
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

// Custom renderer using Theme colors
internal class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    public ModernMenuRenderer() : base(new ModernColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            using var brush = new SolidBrush(Theme.MenuHover);
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
    }
}

internal class ModernColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Theme.MenuBorder;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Theme.MenuHover;
    public override Color MenuStripGradientBegin => Theme.MenuBg;
    public override Color MenuStripGradientEnd => Theme.MenuBg;
    public override Color ToolStripDropDownBackground => Theme.MenuBg;
    public override Color ImageMarginGradientBegin => Theme.MenuBg;
    public override Color ImageMarginGradientMiddle => Theme.MenuBg;
    public override Color ImageMarginGradientEnd => Theme.MenuBg;
    public override Color SeparatorDark => Theme.SeparatorColor;
    public override Color SeparatorLight => Theme.MenuBg;
}

using System.Drawing;

namespace Pass.Services;

public static class Theme
{
    private static bool _isDark;

    public static bool IsDark => _isDark;

    // Surface colors
    public static Color BgColor { get; private set; }
    public static Color SurfaceColor { get; private set; }
    public static Color BorderColor { get; private set; }
    public static Color CardColor { get; private set; }

    // Text colors
    public static Color TextColor { get; private set; }
    public static Color LabelColor { get; private set; }

    // Accent colors
    public static Color AccentColor { get; private set; }
    public static Color AccentHover { get; private set; }

    // Danger
    public static Color DangerColor { get; private set; }

    // List
    public static Color ListSelectColor { get; private set; }

    // Menu
    public static Color MenuBg { get; private set; }
    public static Color MenuHover { get; private set; }
    public static Color MenuBorder { get; private set; }
    public static Color SeparatorColor { get; private set; }

    // Special
    public static Color GoldColor { get; private set; }

    // Input
    public static Color InputBg { get; private set; }
    public static Color InputBorder { get; private set; }

    // Button
    public static Color SmallButtonBg { get; private set; }
    public static Color SmallButtonHover { get; private set; }
    public static Color DangerHover { get; private set; }
    public static Color OutlineHover { get; private set; }

    static Theme()
    {
        ApplyLight();
    }

    public static void SetDarkMode(bool dark)
    {
        _isDark = dark;
        if (dark) ApplyDark(); else ApplyLight();
    }

    private static void ApplyLight()
    {
        BgColor = Color.FromArgb(245, 245, 245);
        SurfaceColor = Color.White;
        BorderColor = Color.FromArgb(218, 220, 224);
        CardColor = Color.White;

        TextColor = Color.FromArgb(51, 51, 51);
        LabelColor = Color.FromArgb(100, 100, 100);

        AccentColor = Color.FromArgb(0, 120, 212);
        AccentHover = Color.FromArgb(0, 100, 180);

        DangerColor = Color.FromArgb(210, 60, 60);

        ListSelectColor = Color.FromArgb(230, 240, 255);

        MenuBg = Color.White;
        MenuHover = Color.FromArgb(230, 240, 255);
        MenuBorder = Color.FromArgb(218, 220, 224);
        SeparatorColor = Color.FromArgb(230, 230, 230);

        GoldColor = Color.FromArgb(218, 165, 32);

        InputBg = Color.White;
        InputBorder = Color.FromArgb(218, 220, 224);

        SmallButtonBg = Color.FromArgb(245, 245, 245);
        SmallButtonHover = Color.FromArgb(230, 230, 230);
        DangerHover = Color.FromArgb(255, 235, 235);
        OutlineHover = Color.FromArgb(230, 240, 255);
    }

    private static void ApplyDark()
    {
        BgColor = Color.FromArgb(30, 30, 30);
        SurfaceColor = Color.FromArgb(45, 45, 45);
        BorderColor = Color.FromArgb(65, 65, 65);
        CardColor = Color.FromArgb(45, 45, 45);

        TextColor = Color.FromArgb(220, 220, 220);
        LabelColor = Color.FromArgb(160, 160, 160);

        AccentColor = Color.FromArgb(60, 150, 230);
        AccentHover = Color.FromArgb(80, 170, 250);

        DangerColor = Color.FromArgb(230, 80, 80);

        ListSelectColor = Color.FromArgb(50, 70, 100);

        MenuBg = Color.FromArgb(40, 40, 40);
        MenuHover = Color.FromArgb(55, 65, 80);
        MenuBorder = Color.FromArgb(65, 65, 65);
        SeparatorColor = Color.FromArgb(60, 60, 60);

        GoldColor = Color.FromArgb(230, 180, 50);

        InputBg = Color.FromArgb(55, 55, 55);
        InputBorder = Color.FromArgb(80, 80, 80);

        SmallButtonBg = Color.FromArgb(55, 55, 55);
        SmallButtonHover = Color.FromArgb(70, 70, 70);
        DangerHover = Color.FromArgb(70, 40, 40);
        OutlineHover = Color.FromArgb(50, 70, 100);
    }
}

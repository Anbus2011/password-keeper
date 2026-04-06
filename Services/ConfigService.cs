using System;
using System.IO;
using System.Text.Json;

namespace Pass.Services;

public static class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pass");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    private static void SaveConfig(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public static string? LoadVaultPath() => LoadConfig().VaultPath;

    public static bool LoadDarkMode() => LoadConfig().DarkMode;

    public static void SaveVaultPath(string vaultPath)
    {
        var config = LoadConfig();
        config.VaultPath = vaultPath;
        SaveConfig(config);
    }

    public static void SaveDarkMode(bool darkMode)
    {
        var config = LoadConfig();
        config.DarkMode = darkMode;
        SaveConfig(config);
    }

    public static (int? x, int? y, int? w, int? h, bool maximized, int? splitter) LoadWindowBounds()
    {
        var c = LoadConfig();
        return (c.WindowX, c.WindowY, c.WindowWidth, c.WindowHeight, c.WindowMaximized, c.SplitterDistance);
    }

    public static void SaveWindowBounds(int x, int y, int w, int h, bool maximized, int splitter)
    {
        var config = LoadConfig();
        config.WindowX = x;
        config.WindowY = y;
        config.WindowWidth = w;
        config.WindowHeight = h;
        config.WindowMaximized = maximized;
        config.SplitterDistance = splitter;
        SaveConfig(config);
    }

    private class AppConfig
    {
        public string? VaultPath { get; set; }
        public bool DarkMode { get; set; }
        public int? WindowX { get; set; }
        public int? WindowY { get; set; }
        public int? WindowWidth { get; set; }
        public int? WindowHeight { get; set; }
        public bool WindowMaximized { get; set; }
        public int? SplitterDistance { get; set; }
    }
}

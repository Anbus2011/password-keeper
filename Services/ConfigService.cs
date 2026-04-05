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

    private class AppConfig
    {
        public string? VaultPath { get; set; }
        public bool DarkMode { get; set; }
    }
}

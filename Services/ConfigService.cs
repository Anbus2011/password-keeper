using System;
using System.IO;
using System.Text.Json;

namespace Pass.Services;

public static class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pass");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static string? LoadVaultPath()
    {
        if (!File.Exists(ConfigPath))
            return null;

        var json = File.ReadAllText(ConfigPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json);
        return config?.VaultPath;
    }

    public static void SaveVaultPath(string vaultPath)
    {
        Directory.CreateDirectory(ConfigDir);
        var config = new AppConfig { VaultPath = vaultPath };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    private class AppConfig
    {
        public string? VaultPath { get; set; }
    }
}

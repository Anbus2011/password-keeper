using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Pass.Models;

namespace Pass.Services;

public class VaultService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private string _vaultPath = "";
    private string _masterPassword = "";
    private List<VaultEntry> _entries = new();

    private string LockPath => _vaultPath + ".lock";
    private string TempPath => _vaultPath + ".tmp";

    public string VaultPath => _vaultPath;
    public bool IsOpen => !string.IsNullOrEmpty(_vaultPath);
    public List<VaultEntry> Entries => _entries;

    /// <summary>
    /// Checks if a lock file exists. Returns lock info string or null.
    /// </summary>
    public static string? CheckLock(string vaultPath)
    {
        var lockPath = vaultPath + ".lock";
        if (!File.Exists(lockPath))
            return null;

        try
        {
            var content = File.ReadAllText(lockPath);
            var parts = content.Split('|', 2);
            if (parts.Length == 2 && DateTime.TryParse(parts[1], out var lockTime))
            {
                var age = DateTime.UtcNow - lockTime;
                if (age.TotalHours > 24)
                    return $"STALE lock from {parts[0]} ({age.TotalHours:F0}h ago — likely a crash)";
                return $"Locked by {parts[0]} since {lockTime:g} UTC";
            }
            return "Unknown lock";
        }
        catch
        {
            return "Lock file exists but cannot be read";
        }
    }

    public void AcquireLock()
    {
        var content = $"{Environment.MachineName}|{DateTime.UtcNow:O}";
        File.WriteAllText(LockPath, content);
    }

    public void ReleaseLock()
    {
        try
        {
            if (File.Exists(LockPath))
                File.Delete(LockPath);
        }
        catch
        {
            // Network may be unavailable at shutdown — ignore
        }
    }

    /// <summary>
    /// Opens a vault file. If file does not exist, creates an empty vault.
    /// Throws CryptographicException if wrong password.
    /// </summary>
    public void Open(string vaultPath, string masterPassword)
    {
        _vaultPath = vaultPath;
        _masterPassword = masterPassword;

        if (!File.Exists(vaultPath))
        {
            _entries = new List<VaultEntry>();
            Save(); // Create the file
            return;
        }

        var fileBytes = File.ReadAllBytes(vaultPath);
        var json = CryptoService.Decrypt(fileBytes, masterPassword);
        _entries = JsonSerializer.Deserialize<List<VaultEntry>>(json, JsonOptions)
                   ?? new List<VaultEntry>();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_entries, JsonOptions);
        var encrypted = CryptoService.Encrypt(json, _masterPassword);

        // Atomic write: write to temp, then replace
        File.WriteAllBytes(TempPath, encrypted);

        if (File.Exists(_vaultPath))
        {
            File.Replace(TempPath, _vaultPath, _vaultPath + ".bak");
            try { File.Delete(_vaultPath + ".bak"); } catch { }
        }
        else
            File.Move(TempPath, _vaultPath);
    }

    public void Dispose()
    {
        ReleaseLock();
        _masterPassword = "";
    }
}

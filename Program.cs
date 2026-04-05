using System;
using System.Security.Cryptography;
using System.Windows.Forms;
using Pass.Forms;
using Pass.Services;

namespace Pass;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        Theme.SetDarkMode(ConfigService.LoadDarkMode());

        var vault = new VaultService();

        var vaultPath = ConfigService.LoadVaultPath();
        if (!string.IsNullOrEmpty(vaultPath) && System.IO.File.Exists(vaultPath))
        {
            // Check lock file
            var lockInfo = VaultService.CheckLock(vaultPath);
            if (lockInfo != null)
            {
                var answer = MessageBox.Show(
                    $"{lockInfo}\n\nOverride lock and open anyway?",
                    "Vault Locked",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                    return;
            }

            // Retry loop for wrong password
            while (true)
            {
                using var loginForm = new LoginForm(isNewVault: false, vaultPath);
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return; // User cancelled — exit app

                try
                {
                    vault.Open(vaultPath, loginForm.MasterPassword);
                    vault.AcquireLock();
                    break; // Success
                }
                catch (CryptographicException)
                {
                    using var errForm = new WrongPasswordForm();
                    var result = errForm.ShowDialog();
                    if (result != DialogResult.OK)
                        return; // User chose Cancel — exit app
                    // OK — loop back to password prompt
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open vault:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        using (vault)
        {
            Application.Run(new MainForm(vault));
        }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fatal error:\n{ex}", "Pass - Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

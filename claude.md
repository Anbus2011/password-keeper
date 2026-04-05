# Pass — Lightweight Password Manager
**Tech:** C# WinForms (.NET 9), zero NuGet dependencies.
**Status:** Implemented, builds clean.

---

## Project Structure
```
Pass\
  Pass.csproj                      -- Project file (.NET 9, WinForms, single-file publish)
  Program.cs                       -- Entry point: config → lock check → login retry loop → main UI
  Models\VaultEntry.cs             -- Data model (Id, Title, Username, Password, Url, Notes, Created, Modified)
  Services\CryptoService.cs       -- PBKDF2 key derivation + AES-256-CBC encrypt/decrypt
  Services\ConfigService.cs       -- Vault path stored in %APPDATA%\Pass\config.json
  Services\VaultService.cs        -- Open/save vault, lock file management, atomic writes
  Forms\LoginForm.cs              -- Master password dialog (with confirm for new vaults)
  Forms\WrongPasswordForm.cs      -- Styled wrong-password dialog (OK to retry, Cancel to exit)
  Forms\MainForm.cs               -- Two-pane UI: search/list + detail editor
```

## Encryption (OpenSSL-portable)
| Parameter | Value |
|---|---|
| KDF | PBKDF2-HMAC-SHA256, 600,000 iterations |
| Salt | 16 bytes random |
| Cipher | AES-256-CBC |
| Padding | PKCS7 |
| IV | 16 bytes random |

**File format:** `[salt 16B][IV 16B][ciphertext NB]`
**Extension:** `.vlt`

Decryptable with OpenSSL given the master password — no app dependency for recovery.

## Lock File
- Path: `vaultPath + ".lock"` — contains `MACHINE_NAME|ISO8601_TIMESTAMP`
- On open: warn if lock exists, let user override or cancel
- Stale locks (>24h) get a softer warning
- Released on app close (best-effort for network failures)

## Startup Flow
1. Check `%APPDATA%\Pass\config.json` for last-opened vault path
2. If vault exists: prompt for master password, retry on wrong password (OK/Cancel), cancel exits app
3. If no vault remembered: open app empty, user uses File > New Vault or File > Open Vault
4. App remembers the last-opened vault for next launch

## UI Theme (Modern Light)
- **Colors:** Light gray background (#F5F5F5), white card surfaces, Windows blue (#0078D4) accents
- **Fonts:** Segoe UI throughout, Semibold for buttons/headers
- **Buttons:** Flat — blue filled (Save/Add, Generate), blue outline (New), red outline (Delete)
- **List:** Owner-drawn with blue accent bar on selection, 30px row height
- **Menu:** Custom renderer — white background, soft blue hover
- **Icon:** 48x48 gold key outline in taskbar/title bar
- **Title bar:** `Pass: VaultName` (no file extension)

## UI Layout (MainForm)
Two-pane `SplitContainer`:
- **Left:** Search TextBox + ListBox of entry titles (scrollable)
- **Right:** White card with fields (Title, Username, Password w/ show checked by default, URL, Notes) + Copy buttons + Generate Password button
- **Button row (bottom-right):** [Delete] [New] [Save/Add]
- **Tab order:** Title → Username → Password → URL → Notes → Save → New → Delete (Copy/Show/Generate skipped)
- **File menu:** New Vault (Ctrl+N), Open Vault (Ctrl+O), Save (Ctrl+S), Close
- Every Save/Add/Delete writes to the encrypted vault file immediately

## Build & Publish
```bash
# Debug build
dotnet build

# Run
dotnet run

# Single-file portable EXE
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```
Output: `bin\Release\net9.0-windows\win-x64\publish\Pass.exe`

Note: `PublishTrimmed` is not compatible with WinForms — omitted intentionally.

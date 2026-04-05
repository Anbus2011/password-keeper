# Pass

A lightweight, portable password manager for Windows. Built with C# WinForms (.NET 9) and zero external dependencies.

Pass encrypts your passwords in a single `.vlt` file that can live on a shared network drive, USB stick, or cloud folder. The file is decryptable with standard tools (OpenSSL, PowerShell) — you're never locked into this app.

## Features

- **AES-256-CBC encryption** with PBKDF2-HMAC-SHA256 key derivation (600,000 iterations)
- **Single encrypted file** — move it anywhere, unlock it with your master password
- **Shared drive safe** — lock file prevents concurrent edits from multiple machines
- **Atomic writes** — vault file is never left in a corrupted state
- **Search** — filter entries instantly by title, username, URL, or notes
- **Password generator** — 20-character random passwords with one click
- **Copy to clipboard** — quick copy buttons for username, password, and URL
- **Portable** — publishes as a single `.exe` with no installer needed
- **OpenSSL recoverable** — the file format is documented; you can decrypt without this app

## Screenshot

*Coming soon*

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for building from source)
- Windows 10/11

### Build and Run

```bash
git clone https://github.com/YOUR_USERNAME/Pass.git
cd Pass
dotnet run
```

### Publish a Portable EXE

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

The output is a single file at `bin\Release\net9.0-windows\win-x64\publish\Pass.exe`. Copy it anywhere and run — no installation required.

## Usage

1. **First launch** — the app opens empty. Use **File > New Vault** to create a `.vlt` file and set a master password.
2. **Add entries** — fill in Title, Username, Password, URL, and Notes, then click **Add**.
3. **Edit entries** — click any entry in the list, modify the fields, click **Save**.
4. **Search** — type in the search box to filter entries instantly.
5. **Next launch** — the app remembers your last vault and prompts for the master password.

## File Format

The `.vlt` file is a binary file with a simple layout:

| Offset | Size | Content |
|--------|------|---------|
| 0 | 16 bytes | Salt (random) |
| 16 | 16 bytes | IV (random) |
| 32 | N bytes | AES-256-CBC ciphertext (PKCS7 padded) |

The plaintext is a JSON array of password entries. The encryption key is derived from your master password using PBKDF2-HMAC-SHA256 with 600,000 iterations.

### Emergency Decryption (without this app)

If you ever need to decrypt the vault file manually:

1. Extract the salt (first 16 bytes) and IV (next 16 bytes) from the file
2. Derive the key: `PBKDF2(password, salt, 600000, SHA256) → 32 bytes`
3. Decrypt: `AES-256-CBC(key, iv, ciphertext) → JSON`

Example with Python:
```python
import hashlib, json
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding

with open("vault.vlt", "rb") as f:
    data = f.read()

salt, iv, ciphertext = data[:16], data[16:32], data[32:]
key = hashlib.pbkdf2_hmac("sha256", b"your_master_password", salt, 600000, dklen=32)

cipher = Cipher(algorithms.AES(key), modes.CBC(iv))
decryptor = cipher.decryptor()
padded = decryptor.update(ciphertext) + decryptor.finalize()

unpadder = padding.PKCS7(128).unpadder()
plaintext = unpadder.update(padded) + unpadder.finalize()

entries = json.loads(plaintext)
for e in entries:
    print(f"{e['title']}: {e['username']} / {e['password']}")
```

## Security Notes

- The master password is held in memory only while the app is running — never written to disk
- Key material is zeroed after each encrypt/decrypt operation
- Each save generates a fresh random salt and IV
- Wrong password detection relies on PKCS7 padding validation (no password hash stored in the file)
- The app config (`%APPDATA%\Pass\config.json`) stores only the vault file path, not the password

## Tech Stack

- **C# / .NET 9** — WinForms for the UI
- **Zero NuGet dependencies** — uses only `System.Security.Cryptography` and `System.Text.Json`
- **Single-file publish** — self-contained portable EXE

## License

MIT

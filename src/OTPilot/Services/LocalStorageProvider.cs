using System.IO;
using System.Security.Cryptography;

namespace OTPilot.Services;

/// <summary>
/// Stores the vault as a DPAPI-encrypted file on the local machine.
/// Encryption is scoped to the current Windows user — no password required.
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _filePath;

    public LocalStorageProvider(string filePath)
    {
        _filePath = filePath;
    }

    public string DisplayName => "Local Storage";
    public bool IsAvailable => true;

    public async Task<byte[]?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var encrypted = await File.ReadAllBytesAsync(_filePath);
        return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
    }

    public async Task SaveAsync(byte[] data)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_filePath, encrypted);
    }
}

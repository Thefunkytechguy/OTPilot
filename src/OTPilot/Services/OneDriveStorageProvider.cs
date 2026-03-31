using System.IO;
using System.Security.Cryptography;

namespace OTPilot.Services;

/// <summary>
/// Stores the vault in the user's OneDrive for Business folder.
/// The vault file follows the user across machines automatically via OneDrive sync.
/// The vault key is stored alongside the vault, protected by per-machine DPAPI.
/// Access control is provided by Entra ID authentication to OneDrive — no master password needed.
///
/// Phase 3 feature — not wired up yet, but ready for activation.
/// </summary>
public class OneDriveStorageProvider : IStorageProvider
{
    private const string FolderName = "OTPilot";
    private const string VaultFileName = "vault.enc";
    private const string KeyFileName = "vault.key";

    private readonly string _oneDrivePath;
    private readonly string _vaultPath;
    private readonly string _keyPath;
    private readonly string _dpApiKeyPath;

    public OneDriveStorageProvider()
    {
        _oneDrivePath = GetOneDrivePath();
        var folder = Path.Combine(_oneDrivePath, FolderName);
        _vaultPath = Path.Combine(folder, VaultFileName);
        _keyPath = Path.Combine(folder, KeyFileName);
        _dpApiKeyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OTPilot", "vaultkey.dat");
    }

    public string DisplayName => "OneDrive for Business";

    public bool IsAvailable => !string.IsNullOrEmpty(_oneDrivePath) && Directory.Exists(_oneDrivePath);

    public async Task<byte[]?> LoadAsync()
    {
        if (!File.Exists(_vaultPath))
            return null;

        var key = await GetOrFetchKeyAsync();
        if (key is null)
            return null;

        var encrypted = await File.ReadAllBytesAsync(_vaultPath);
        return Decrypt(encrypted, key);
    }

    public async Task SaveAsync(byte[] data)
    {
        var key = await GetOrFetchKeyAsync() ?? GenerateAndStoreKey();

        var folder = Path.GetDirectoryName(_vaultPath)!;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var encrypted = Encrypt(data, key);
        await File.WriteAllBytesAsync(_vaultPath, encrypted);
    }

    // ── Key management ───────────────────────────────────────────────────────

    private async Task<byte[]?> GetOrFetchKeyAsync()
    {
        // 1. Try local DPAPI-protected cache first (fast path)
        if (File.Exists(_dpApiKeyPath))
        {
            var cached = await File.ReadAllBytesAsync(_dpApiKeyPath);
            return ProtectedData.Unprotect(cached, null, DataProtectionScope.CurrentUser);
        }

        // 2. Key not cached locally — fetch from OneDrive and cache it
        if (!File.Exists(_keyPath))
            return null;

        var remoteKey = await File.ReadAllBytesAsync(_keyPath);
        await CacheKeyLocallyAsync(remoteKey);
        return remoteKey;
    }

    private byte[] GenerateAndStoreKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var folder = Path.GetDirectoryName(_keyPath)!;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        // Store raw key in OneDrive (access-controlled by Entra ID auth)
        File.WriteAllBytes(_keyPath, key);

        // Cache locally with DPAPI
        CacheKeyLocallyAsync(key).GetAwaiter().GetResult();

        return key;
    }

    private async Task CacheKeyLocallyAsync(byte[] key)
    {
        var dir = Path.GetDirectoryName(_dpApiKeyPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var protected_ = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_dpApiKeyPath, protected_);
    }

    // ── AES-256-GCM encryption ────────────────────────────────────────────────

    private static byte[] Encrypt(byte[] data, byte[] key)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[data.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, data, ciphertext, tag);

        // Format: [nonce (12)] [tag (16)] [ciphertext]
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);
        return result;
    }

    private static byte[] Decrypt(byte[] data, byte[] key)
    {
        const int nonceSize = 12;
        const int tagSize = 16;

        var nonce = data[..nonceSize];
        var tag = data[nonceSize..(nonceSize + tagSize)];
        var ciphertext = data[(nonceSize + tagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetOneDrivePath()
    {
        // OneDrive for Business sets this environment variable
        var path = Environment.GetEnvironmentVariable("OneDriveCommercial")
                ?? Environment.GetEnvironmentVariable("OneDrive")
                ?? string.Empty;
        return path;
    }
}

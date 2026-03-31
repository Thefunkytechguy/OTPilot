using System.Text;
using System.Text.Json;
using OTPilot.Models;

namespace OTPilot.Services;

public class VaultService
{
    private readonly IStorageProvider _storage;
    private Vault _vault = new();

    public VaultService(IStorageProvider storage)
    {
        _storage = storage;
    }

    public IReadOnlyList<TotpAccount> Accounts => _vault.Accounts.AsReadOnly();
    public string StorageDisplayName => _storage.DisplayName;

    public async Task LoadAsync()
    {
        try
        {
            var data = await _storage.LoadAsync();
            if (data is null) return;

            var json = Encoding.UTF8.GetString(data);
            _vault = JsonSerializer.Deserialize<Vault>(json) ?? new Vault();
        }
        catch
        {
            // Vault missing or corrupt — start fresh
            _vault = new Vault();
        }
    }

    public async Task AddAccountAsync(TotpAccount account)
    {
        _vault.Accounts.Add(account);
        await PersistAsync();
    }

    public async Task RemoveAccountAsync(string id)
    {
        _vault.Accounts.RemoveAll(a => a.Id == id);
        await PersistAsync();
    }

    public async Task UpdateAccountAsync(TotpAccount account)
    {
        var index = _vault.Accounts.FindIndex(a => a.Id == account.Id);
        if (index >= 0)
        {
            _vault.Accounts[index] = account;
            await PersistAsync();
        }
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_vault);
        var data = Encoding.UTF8.GetBytes(json);
        await _storage.SaveAsync(data);
    }
}

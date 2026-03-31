namespace OTPilot.Services;

public interface IStorageProvider
{
    string DisplayName { get; }
    bool IsAvailable { get; }
    Task<byte[]?> LoadAsync();
    Task SaveAsync(byte[] data);
}

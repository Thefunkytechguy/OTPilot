namespace OTPilot.Models;

public class Vault
{
    public int Version { get; set; } = 1;
    public List<TotpAccount> Accounts { get; set; } = new();
}

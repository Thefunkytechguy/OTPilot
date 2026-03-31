namespace OTPilot.Models;

public class TotpAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Issuer { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "SHA1";
    public int Digits { get; set; } = 6;
    public int Period { get; set; } = 30;
}

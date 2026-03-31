using OTPilot.Models;
using OtpNet;

namespace OTPilot.Services;

public class TotpService
{
    public string GenerateCode(TotpAccount account)
    {
        var secretBytes = Base32Encoding.ToBytes(account.Secret);
        var totp = new Totp(secretBytes, account.Period, GetHashMode(account.Algorithm), account.Digits);
        return totp.ComputeTotp();
    }

    public int GetSecondsRemaining(int period = 30)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return period - (int)(epoch % period);
    }

    private static OtpHashMode GetHashMode(string algorithm) => algorithm.ToUpperInvariant() switch
    {
        "SHA256" => OtpHashMode.Sha256,
        "SHA512" => OtpHashMode.Sha512,
        _ => OtpHashMode.Sha1
    };
}

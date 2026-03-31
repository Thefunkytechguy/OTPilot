using System.Web;
using OTPilot.Models;

namespace OTPilot.Services;

public static class OtpAuthUriParser
{
    public static TotpAccount? Parse(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        if (!uri.StartsWith("otpauth://totp/", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var u = new Uri(uri);
            var label = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/'));
            var query = HttpUtility.ParseQueryString(u.Query);

            var secret = query["secret"];
            if (string.IsNullOrWhiteSpace(secret))
                return null;

            // Clean the secret — remove spaces and uppercase
            secret = secret.Replace(" ", "").ToUpperInvariant();

            var issuer = query["issuer"] ?? string.Empty;
            var accountName = label;

            // Label format: "Issuer:AccountName" or just "AccountName"
            if (label.Contains(':'))
            {
                var parts = label.Split(':', 2);
                if (string.IsNullOrEmpty(issuer))
                    issuer = parts[0].Trim();
                accountName = parts[1].Trim();
            }

            return new TotpAccount
            {
                Issuer = issuer,
                AccountName = accountName,
                Secret = secret,
                Algorithm = query["algorithm"] ?? "SHA1",
                Digits = int.TryParse(query["digits"], out var d) ? d : 6,
                Period = int.TryParse(query["period"], out var p) ? p : 30
            };
        }
        catch
        {
            return null;
        }
    }
}

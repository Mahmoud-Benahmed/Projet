using System.Security.Cryptography;
using System.Text;

public class PwnedPasswordService
{
    private readonly HttpClient _http;
    private readonly ILogger<PwnedPasswordService> _logger;

    public PwnedPasswordService(HttpClient http, ILogger<PwnedPasswordService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if the password appears in the HaveIBeenPwned breach database.
    /// Uses k-Anonymity — only the first 5 chars of the SHA-1 hash are sent.
    /// </summary>
    public async Task<bool> IsPwnedAsync(string password)
    {
        var hash = Sha1Hex(password);
        var prefix = hash[..5];
        var suffix = hash[5..];

        try
        {
            var response = await _http.GetStringAsync(
                $"https://api.pwnedpasswords.com/range/{prefix}"
            );

            return response
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Any(line =>
                {
                    var parts = line.Split(':');
                    return parts.Length == 2
                        && parts[0].Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(parts[1].Trim(), out var count)
                        && count > 0;
                });
        }
        catch (Exception ex)
        {
            // Fail open — don't block legitimate users if the API is down
            _logger.LogWarning(ex, "Pwned Passwords API unavailable — skipping breach check");
            return false;
        }
    }

    private static string Sha1Hex(string input)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes); // uppercase by default
    }
}
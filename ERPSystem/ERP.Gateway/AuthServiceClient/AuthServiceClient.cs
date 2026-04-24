// Services/IAuthServiceClient.cs
namespace ERP.Gateway.AuthServiceClient;

public interface IAuthServiceClient
{
    Task<TokenValidationResponse> ValidateTokenAsync(string token);
}

// Services/AuthServiceClient.cs
public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TokenValidationResponse> ValidateTokenAsync(string token)
    {
        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/auth/validate-token");
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                TokenValidationResponse? result = await response.Content.ReadFromJsonAsync<TokenValidationResponse>();
                return result ?? TokenValidationResponse.Invalid("Invalid response from auth service");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return TokenValidationResponse.Invalid(error?.reason ?? "Token validation failed");
            }

            _logger.LogWarning("AuthService returned {StatusCode} for token validation", response.StatusCode);
            return TokenValidationResponse.Invalid("Auth service validation failed");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to AuthService for token validation");
            return TokenValidationResponse.Invalid("Auth service unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return TokenValidationResponse.Invalid("Validation service error");
        }
    }
}

// Models/TokenValidationResponse.cs
public class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public TokenValidationUser? User { get; set; }

    public static TokenValidationResponse Valid(TokenValidationUser user)
    {
        return new TokenValidationResponse
        {
            IsValid = true,
            User = user
        };
    }

    public static TokenValidationResponse Invalid(string reason)
    {
        return new TokenValidationResponse
        {
            IsValid = false,
            Reason = reason
        };
    }
}

public class TokenValidationUser
{
    public Guid UserId { get; set; }
    public string? Login { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
}

public class ErrorResponse
{
    public string? reason { get; set; }
    public bool isValid { get; set; }
}
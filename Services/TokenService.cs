using System.Text.Json;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.Extensions.Options;

namespace ComricFraudCalculatorBackend.Services;

public class AzureAdOptions
{
    public const string SectionName = "AzureAd";
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public interface ITokenService
{
    Task<TokenResponse> ExchangeCredentialsAsync(TokenRequest request, CancellationToken ct);
}

public class TokenService(IHttpClientFactory httpClientFactory, IOptions<AzureAdOptions> azureAdOptions) : ITokenService
{
    public async Task<TokenResponse> ExchangeCredentialsAsync(TokenRequest request, CancellationToken ct)
    {
        var options = azureAdOptions.Value;
        var tokenEndpoint = $"{options.Instance.TrimEnd('/')}/{options.TenantId}/oauth2/v2.0/token";

        var client = httpClientFactory.CreateClient();
        var scope = string.IsNullOrWhiteSpace(request.Scope)
            ? $"api://{options.ClientId}/.default"
            : string.Join(' ', request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.StartsWith("api://", StringComparison.OrdinalIgnoreCase)
                    ? s
                    : $"api://{options.ClientId}/{s}"));

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = request.ClientId,
            ["client_secret"] = request.ClientSecret,
            ["scope"] = scope
        });

        using var response = await client.PostAsync(tokenEndpoint, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedAccessException($"Token exchange failed: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return new TokenResponse(
            root.GetProperty("access_token").GetString()!,
            root.GetProperty("token_type").GetString() ?? "Bearer",
            root.GetProperty("expires_in").GetInt32(),
            root.TryGetProperty("scope", out var scopeElement) ? scopeElement.GetString() ?? request.Scope : request.Scope);
    }
}

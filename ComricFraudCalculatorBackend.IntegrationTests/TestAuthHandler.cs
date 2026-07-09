using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ComricFraudCalculatorBackend.IntegrationTests;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthScheme = "Test";

    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var tenantId = Request.Headers.TryGetValue("X-Test-TenantId", out var tenantHeader)
            ? tenantHeader.ToString()
            : DefaultTenantId.ToString();

        var scopes = Request.Headers.TryGetValue("X-Test-Scopes", out var scopeHeader)
            ? scopeHeader.ToString()
            : "Events.Read Events.Write Signals.Read Audit.Read Dashboard.Read";

        var claims = new List<Claim>
        {
            new("tenant_id", tenantId),
            new("scp", scopes)
        };

        var identity = new ClaimsIdentity(claims, AuthScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

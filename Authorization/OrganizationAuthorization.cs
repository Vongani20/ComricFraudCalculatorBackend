using System.Security.Claims;

namespace ComricFraudCalculatorBackend.Authorization;

public static class OrganizationAuthorization
{
    public const string DefaultAllowedEmailDomain = "solugrowth.com";

    public static bool HasAllowedEmailDomain(ClaimsPrincipal user, string? allowedDomain)
    {
        var domain = NormalizeDomain(allowedDomain);
        if (domain is null)
            return true;

        var suffix = $"@{domain}";
        foreach (var claimType in EmailClaimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (IsAllowedEmail(value, suffix))
                return true;
        }

        // Access tokens may surface the UPN/email on an uncommon claim type.
        foreach (var claim in user.Claims)
        {
            if (IsAllowedEmail(claim.Value, suffix))
                return true;
        }

        return false;
    }

    private static bool IsAllowedEmail(string? value, string suffix)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@', StringComparison.Ordinal))
            return false;

        return value.Trim().EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizeDomain(string? allowedDomain)
    {
        if (string.IsNullOrWhiteSpace(allowedDomain))
            return null;

        return allowedDomain.Trim().TrimStart('@').ToLowerInvariant();
    }

    private static readonly string[] EmailClaimTypes =
    [
        "preferred_username",
        "upn",
        "email",
        "unique_name",
        "name",
        ClaimTypes.Upn,
        ClaimTypes.Email,
        ClaimTypes.Name,
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
    ];
}

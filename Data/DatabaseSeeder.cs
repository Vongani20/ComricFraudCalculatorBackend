using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Enums;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
            return;

        var vodacom = new Tenant
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TenantName = "Vodacom",
            TenantCode = "VOD",
            IsActive = true,
            SubscriptionTier = SubscriptionTier.Premium,
            CreatedAt = DateTime.UtcNow
        };

        var mtn = new Tenant
        {
            TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TenantName = "MTN",
            TenantCode = "MTN",
            IsActive = true,
            SubscriptionTier = SubscriptionTier.Premium,
            CreatedAt = DateTime.UtcNow
        };

        db.Tenants.AddRange(vodacom, mtn);
        await db.SaveChangesAsync(ct);
    }
}

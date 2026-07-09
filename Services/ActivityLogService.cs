using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Services;

public interface IActivityLogService
{
    Task LogAsync(string action, string endpoint, string httpMethod, int statusCode, string? clientIp, CancellationToken ct);
    Task<ActivityLogListResponse> ListAsync(int page, int pageSize, CancellationToken ct);
}

public class ActivityLogService(ApplicationDbContext db, ITenantProvider tenantProvider) : IActivityLogService
{
    public async Task LogAsync(string action, string endpoint, string httpMethod, int statusCode, string? clientIp, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId is null)
            return;

        db.ActivityLogs.Add(new ActivityLog
        {
            ActivityLogId = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = action,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            StatusCode = statusCode,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<ActivityLogListResponse> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        var query = db.ActivityLogs.AsNoTracking();
        if (tenantId is not null)
            query = query.Where(a => a.TenantId == tenantId);

        var totalCount = await query.CountAsync(ct);

        var entries = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ActivityLogListResponse(
            entries.Select(a => new ActivityLogResponse(
                a.ActivityLogId, a.Action, a.Endpoint, a.HttpMethod,
                a.StatusCode, a.ClientIp, a.CreatedAt)).ToList(),
            totalCount,
            page,
            pageSize);
    }
}

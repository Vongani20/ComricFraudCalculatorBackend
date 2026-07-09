using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Services;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetStatsAsync(CancellationToken ct);
    Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken ct);
}

public class DashboardService(ApplicationDbContext db, ITenantProvider tenantProvider) : IDashboardService
{
    private const int HighRiskThreshold = 70;

    public async Task<DashboardStatsResponse> GetStatsAsync(CancellationToken ct)
    {
        var overview = await BuildStatsAsync(ct);
        return overview;
    }

    public async Task<DashboardOverviewResponse> GetOverviewAsync(CancellationToken ct)
    {
        var stats = await BuildStatsAsync(ct);
        var tenantId = tenantProvider.GetTenantId();

        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);
        var hrEvents = await FilterByTenant(db.HrEvents.AsNoTracking(), tenantId)
            .Where(e => e.CreatedAt >= thirtyDaysAgo)
            .Select(e => new { e.CreatedAt })
            .ToListAsync(ct);

        var mnoEvents = await FilterByTenant(db.MnoEvents.AsNoTracking(), tenantId)
            .Where(e => e.CreatedAt >= thirtyDaysAgo)
            .Select(e => new { e.CreatedAt })
            .ToListAsync(ct);

        var activitySeries = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var day = thirtyDaysAgo.AddDays(offset);
                var nextDay = day.AddDays(1);
                return new DashboardActivityPoint(
                    day.ToString("yyyy-MM-dd"),
                    hrEvents.Count(e => e.CreatedAt >= day && e.CreatedAt < nextDay),
                    mnoEvents.Count(e => e.CreatedAt >= day && e.CreatedAt < nextDay));
            })
            .ToList();

        var recentHr = await FilterByTenant(db.HrEvents.AsNoTracking(), tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .Select(e => new RecentSubmissionResponse(
                e.EventId,
                "HR",
                e.IdNumber,
                e.EventType.ToString(),
                e.RiskScore,
                e.VerificationStatus.ToString(),
                e.CreatedAt))
            .ToListAsync(ct);

        var recentMno = await FilterByTenant(db.MnoEvents.AsNoTracking(), tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .Select(e => new RecentSubmissionResponse(
                e.EventId,
                "MNO",
                e.IdNumber,
                e.EventType.ToString(),
                e.RiskScore,
                e.RiskScore >= HighRiskThreshold ? "High Risk" : "Normal",
                e.CreatedAt))
            .ToListAsync(ct);

        var recentSubmissions = recentHr
            .Concat(recentMno)
            .OrderByDescending(e => e.SubmittedAt)
            .Take(10)
            .ToList();

        var topSignals = await db.Signals
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.AggregateRiskScore)
            .ThenByDescending(s => s.LastSeen)
            .Take(5)
            .Select(s => new FraudSignalResponse(
                s.SignalId,
                s.IdNumberHash,
                s.SignalType,
                s.SignalCategory,
                s.OccurrenceCount,
                s.FirstSeen,
                s.LastSeen,
                s.AggregateRiskScore,
                s.IsActive))
            .ToListAsync(ct);

        return new DashboardOverviewResponse(stats, activitySeries, recentSubmissions, topSignals);
    }

    private async Task<DashboardStatsResponse> BuildStatsAsync(CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        var today = DateTime.UtcNow.Date;

        var hrQuery = FilterByTenant(db.HrEvents.AsNoTracking(), tenantId);
        var mnoQuery = FilterByTenant(db.MnoEvents.AsNoTracking(), tenantId);
        var logQuery = FilterByTenant(db.ActivityLogs.AsNoTracking(), tenantId);

        var hrCount = await hrQuery.CountAsync(ct);
        var mnoCount = await mnoQuery.CountAsync(ct);
        var activeSignals = await db.Signals.CountAsync(s => s.IsActive, ct);

        var hrHighRisk = await hrQuery.CountAsync(e => e.RiskScore > HighRiskThreshold, ct);
        var mnoHighRisk = await mnoQuery.CountAsync(e => e.RiskScore > HighRiskThreshold, ct);
        var apiCallsToday = await logQuery.CountAsync(a => a.CreatedAt >= today, ct);

        return new DashboardStatsResponse(
            hrCount + mnoCount,
            activeSignals,
            hrHighRisk + mnoHighRisk,
            apiCallsToday,
            hrCount,
            mnoCount);
    }

    private static IQueryable<T> FilterByTenant<T>(IQueryable<T> query, Guid? tenantId) where T : class, ITenantScoped
        => tenantId is null ? query : query.Where(e => e.TenantId == tenantId);
}

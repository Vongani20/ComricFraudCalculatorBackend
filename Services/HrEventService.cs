using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Services;

public interface IHrEventService
{
    Task<IReadOnlyList<HrEventResponse>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<HrEventResponse> SubmitAsync(SubmitHrEventRequest request, CancellationToken ct);
}

public class HrEventService(
    ApplicationDbContext db,
    ITenantProvider tenantProvider,
    IHashingService hashingService,
    IRiskScoreService riskScoreService,
    IFraudSignalService signalService) : IHrEventService
{
    public async Task<IReadOnlyList<HrEventResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        var query = db.HrEvents.AsNoTracking();
        if (tenantId is not null)
            query = query.Where(e => e.TenantId == tenantId);

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return events.Select(Map).ToList();
    }

    public async Task<HrEventResponse> SubmitAsync(SubmitHrEventRequest request, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var riskScore = riskScoreService.CalculateHrRiskScore(request.EventType, request.VerificationStatus);

        var hrEvent = new HrEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            IdNumber = request.IdNumber,
            EventType = request.EventType,
            EventDate = request.EventDate,
            EmployerName = request.EmployerName,
            EmployeeNumber = request.EmployeeNumber,
            VerificationStatus = request.VerificationStatus,
            RiskScore = riskScore,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        db.HrEvents.Add(hrEvent);

        await signalService.UpsertFromHrEventAsync(
            hashingService.HashIdNumber(request.IdNumber),
            request.EventType,
            request.EventDate,
            riskScore,
            ct);

        await db.SaveChangesAsync(ct);
        return Map(hrEvent);
    }

    private static HrEventResponse Map(HrEvent e) => new(
        e.EventId, e.IdNumber, e.EventType, e.EventDate, e.EmployerName,
        e.EmployeeNumber, e.VerificationStatus, e.RiskScore, e.Notes, e.CreatedAt);
}

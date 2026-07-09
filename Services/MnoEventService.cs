using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Services;

public interface IMnoEventService
{
    Task<IReadOnlyList<MnoEventResponse>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<MnoEventResponse> SubmitAsync(SubmitMnoEventRequest request, CancellationToken ct);
}

public class MnoEventService(
    ApplicationDbContext db,
    ITenantProvider tenantProvider,
    IHashingService hashingService,
    IRiskScoreService riskScoreService,
    IFraudSignalService signalService) : IMnoEventService
{
    public async Task<IReadOnlyList<MnoEventResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        var query = db.MnoEvents.AsNoTracking();
        if (tenantId is not null)
            query = query.Where(e => e.TenantId == tenantId);

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return events.Select(Map).ToList();
    }

    public async Task<MnoEventResponse> SubmitAsync(SubmitMnoEventRequest request, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var riskScore = riskScoreService.CalculateMnoRiskScore(request.EventType, request.FlagReason);

        var mnoEvent = new MnoEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            IdNumber = request.IdNumber,
            Msisdn = request.Msisdn,
            EventType = request.EventType,
            EventDate = request.EventDate,
            ApplicationChannel = request.ApplicationChannel,
            OutletOrDealer = request.OutletOrDealer,
            DeviceImei = request.DeviceImei,
            RiskScore = riskScore,
            FlagReason = request.FlagReason,
            CreatedAt = DateTime.UtcNow
        };

        db.MnoEvents.Add(mnoEvent);

        await signalService.UpsertFromMnoEventAsync(
            hashingService.HashIdNumber(request.IdNumber),
            request.EventType,
            request.EventDate,
            riskScore,
            ct);

        await db.SaveChangesAsync(ct);
        return Map(mnoEvent);
    }

    private static MnoEventResponse Map(MnoEvent e) => new(
        e.EventId, e.IdNumber, e.Msisdn, e.EventType, e.EventDate,
        e.ApplicationChannel, e.OutletOrDealer, e.DeviceImei, e.RiskScore, e.FlagReason, e.CreatedAt);
}

using ComricFraudCalculatorBackend.Data;
using ComricFraudCalculatorBackend.Entities;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ComricFraudCalculatorBackend.Services;

public interface IFraudSignalService
{
    Task<FraudSignalListResponse> ListAsync(int page, int pageSize, bool? activeOnly, CancellationToken ct);
    Task<FraudSignalResponse?> GetByHashAsync(string idNumberHash, CancellationToken ct);
    Task<IdCheckResponse> CheckIdAsync(string idNumber, CancellationToken ct);
    Task UpsertFromHrEventAsync(string idNumberHash, HrEventType eventType, DateTime eventDate, int riskScore, CancellationToken ct);
    Task UpsertFromMnoEventAsync(string idNumberHash, MnoEventType eventType, DateTime eventDate, int riskScore, CancellationToken ct);
}

public class FraudSignalService(ApplicationDbContext db, IHashingService hashingService, IRiskScoreService riskScoreService) : IFraudSignalService
{
    public async Task<FraudSignalListResponse> ListAsync(int page, int pageSize, bool? activeOnly, CancellationToken ct)
    {
        var query = db.Signals.AsNoTracking();

        if (activeOnly == true)
            query = query.Where(s => s.IsActive);

        var totalCount = await query.CountAsync(ct);

        var signals = await query
            .OrderByDescending(s => s.AggregateRiskScore)
            .ThenByDescending(s => s.LastSeen)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new FraudSignalListResponse(
            signals.Select(Map).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<FraudSignalResponse?> GetByHashAsync(string idNumberHash, CancellationToken ct)
    {
        var signal = await db.Signals
            .AsNoTracking()
            .Where(s => s.IdNumberHash == idNumberHash)
            .OrderByDescending(s => s.AggregateRiskScore)
            .FirstOrDefaultAsync(ct);

        return signal is null ? null : Map(signal);
    }

    public async Task<IdCheckResponse> CheckIdAsync(string idNumber, CancellationToken ct)
    {
        var hash = hashingService.HashIdNumber(idNumber);
        var signals = await db.Signals
            .AsNoTracking()
            .Where(s => s.IdNumberHash == hash && s.IsActive)
            .OrderByDescending(s => s.AggregateRiskScore)
            .ToListAsync(ct);

        return new IdCheckResponse(hash, signals.Count > 0, signals.Select(Map).ToList());
    }

    public Task UpsertFromHrEventAsync(string idNumberHash, HrEventType eventType, DateTime eventDate, int riskScore, CancellationToken ct)
    {
        var category = MapHrCategory(eventType);
        return UpsertSignalAsync(idNumberHash, SignalType.HR_Alert, category, eventDate, riskScore, ct);
    }

    public Task UpsertFromMnoEventAsync(string idNumberHash, MnoEventType eventType, DateTime eventDate, int riskScore, CancellationToken ct)
    {
        var category = MapMnoCategory(eventType);
        return UpsertSignalAsync(idNumberHash, SignalType.MNO_Alert, category, eventDate, riskScore, ct);
    }

    private async Task UpsertSignalAsync(
        string idNumberHash,
        SignalType signalType,
        SignalCategory category,
        DateTime eventDate,
        int riskScore,
        CancellationToken ct)
    {
        var signal = await db.Signals
            .FirstOrDefaultAsync(s =>
                s.IdNumberHash == idNumberHash &&
                s.SignalType == signalType &&
                s.SignalCategory == category, ct);

        if (signal is null)
        {
            db.Signals.Add(new Signal
            {
                SignalId = Guid.NewGuid(),
                IdNumberHash = idNumberHash,
                SignalType = signalType,
                SignalCategory = category,
                OccurrenceCount = 1,
                FirstSeen = eventDate,
                LastSeen = eventDate,
                AggregateRiskScore = riskScore,
                IsActive = true
            });
            return;
        }

        signal.OccurrenceCount++;
        signal.FirstSeen = eventDate < signal.FirstSeen ? eventDate : signal.FirstSeen;
        signal.LastSeen = eventDate > signal.LastSeen ? eventDate : signal.LastSeen;
        signal.AggregateRiskScore = riskScoreService.AggregateSignalScore(
            signal.AggregateRiskScore, riskScore, signal.OccurrenceCount);
        signal.IsActive = true;
    }

    private static SignalCategory MapHrCategory(HrEventType eventType) => eventType switch
    {
        HrEventType.GhostEmployee or HrEventType.PayrollMismatch => SignalCategory.EmploymentAnomaly,
        HrEventType.IdentityFraud => SignalCategory.IdentityMismatch,
        _ => SignalCategory.EmploymentAnomaly
    };

    private static SignalCategory MapMnoCategory(MnoEventType eventType) => eventType switch
    {
        MnoEventType.SIMSwap or MnoEventType.NewSIMApplication => SignalCategory.SIMVelocity,
        MnoEventType.PortRequest => SignalCategory.PortingRisk,
        _ => SignalCategory.IdentityMismatch
    };

    private static FraudSignalResponse Map(Signal s) => new(
        s.SignalId, s.IdNumberHash, s.SignalType, s.SignalCategory,
        s.OccurrenceCount, s.FirstSeen, s.LastSeen, s.AggregateRiskScore, s.IsActive);
}

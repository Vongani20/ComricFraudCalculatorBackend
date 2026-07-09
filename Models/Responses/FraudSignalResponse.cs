using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Models.Responses;

public record FraudSignalResponse(
    Guid SignalId,
    string IdNumberHash,
    SignalType SignalType,
    SignalCategory SignalCategory,
    int OccurrenceCount,
    DateTime FirstSeen,
    DateTime LastSeen,
    int AggregateRiskScore,
    bool IsActive);

public record FraudSignalListResponse(
    IReadOnlyList<FraudSignalResponse> Signals,
    int TotalCount,
    int Page,
    int PageSize);

using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Models.Responses;

public record MnoEventResponse(
    Guid EventId,
    string IdNumber,
    string Msisdn,
    MnoEventType EventType,
    DateTime EventDate,
    ApplicationChannel ApplicationChannel,
    string OutletOrDealer,
    string? DeviceImei,
    int RiskScore,
    string? FlagReason,
    DateTime CreatedAt);

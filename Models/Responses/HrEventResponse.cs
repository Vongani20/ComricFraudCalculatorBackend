using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Models.Responses;

public record HrEventResponse(
    Guid EventId,
    string IdNumber,
    HrEventType EventType,
    DateTime EventDate,
    string EmployerName,
    string? EmployeeNumber,
    VerificationStatus VerificationStatus,
    int RiskScore,
    string? Notes,
    DateTime CreatedAt);

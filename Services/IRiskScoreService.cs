using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Services;

public interface IRiskScoreService
{
    int CalculateHrRiskScore(HrEventType eventType, VerificationStatus status);
    int CalculateMnoRiskScore(MnoEventType eventType, string? flagReason);
    int AggregateSignalScore(int currentScore, int newEventScore, int occurrenceCount);
}

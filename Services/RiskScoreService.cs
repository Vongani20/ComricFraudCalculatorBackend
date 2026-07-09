using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Services;

public class RiskScoreService : IRiskScoreService
{
    public int CalculateHrRiskScore(HrEventType eventType, VerificationStatus status)
    {
        var baseScore = eventType switch
        {
            HrEventType.GhostEmployee => 75,
            HrEventType.IdentityFraud => 85,
            HrEventType.PayrollMismatch => 60,
            HrEventType.EmployeeVerification => 30,
            _ => 40
        };

        var statusModifier = status switch
        {
            VerificationStatus.Denied => 15,
            VerificationStatus.Confirmed => -10,
            VerificationStatus.Inconclusive => 5,
            _ => 0
        };

        return Math.Clamp(baseScore + statusModifier, 0, 100);
    }

    public int CalculateMnoRiskScore(MnoEventType eventType, string? flagReason)
    {
        var baseScore = eventType switch
        {
            MnoEventType.SIMSwap => 80,
            MnoEventType.PortRequest => 70,
            MnoEventType.NewSIMApplication => 50,
            MnoEventType.ContractApplication => 45,
            MnoEventType.RICARegistration => 40,
            _ => 40
        };

        if (!string.IsNullOrWhiteSpace(flagReason))
        {
            if (flagReason.Contains("velocity", StringComparison.OrdinalIgnoreCase))
                baseScore += 20;
            if (flagReason.Contains("mismatch", StringComparison.OrdinalIgnoreCase))
                baseScore += 15;
            if (flagReason.Contains("fraud", StringComparison.OrdinalIgnoreCase))
                baseScore += 10;
        }

        return Math.Clamp(baseScore, 0, 100);
    }

    public int AggregateSignalScore(int currentScore, int newEventScore, int occurrenceCount)
    {
        var recencyWeight = Math.Min(occurrenceCount, 10) / 10.0;
        var aggregated = (int)Math.Round(currentScore * 0.6 + newEventScore * 0.4 * recencyWeight + occurrenceCount * 2);
        return Math.Clamp(aggregated, 0, 100);
    }
}

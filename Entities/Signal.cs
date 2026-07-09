using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Entities;

public class Signal
{
    public Guid SignalId { get; set; }
    public string IdNumberHash { get; set; } = string.Empty;
    public SignalType SignalType { get; set; }
    public SignalCategory SignalCategory { get; set; }
    public int OccurrenceCount { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int AggregateRiskScore { get; set; }
    public bool IsActive { get; set; } = true;
}

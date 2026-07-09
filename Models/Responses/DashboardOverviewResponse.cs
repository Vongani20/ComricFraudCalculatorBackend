namespace ComricFraudCalculatorBackend.Models.Responses;

public record DashboardStatsResponse(
    int TotalEventsSubmitted,
    int ActiveSignals,
    int HighRiskAlerts,
    int ApiCallsToday,
    int TotalHrEvents,
    int TotalMnoEvents);

public record DashboardActivityPoint(
    string Date,
    int HrCount,
    int MnoCount);

public record RecentSubmissionResponse(
    Guid EventId,
    string Source,
    string IdNumber,
    string EventType,
    int RiskScore,
    string Status,
    DateTime SubmittedAt);

public record DashboardOverviewResponse(
    DashboardStatsResponse Stats,
    IReadOnlyList<DashboardActivityPoint> ActivitySeries,
    IReadOnlyList<RecentSubmissionResponse> RecentSubmissions,
    IReadOnlyList<FraudSignalResponse> TopFraudSignals);

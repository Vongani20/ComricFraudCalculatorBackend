namespace ComricFraudCalculatorBackend.Authorization;

public static class AuthScopes
{
    public const string EventsRead = "Events.Read";
    public const string EventsWrite = "Events.Write";
    public const string SignalsRead = "Signals.Read";
    public const string AuditRead = "Audit.Read";
    public const string DashboardRead = "Dashboard.Read";
}

public static class AuthPolicies
{
    public const string EventsRead = "EventsRead";
    public const string EventsWrite = "EventsWrite";
    public const string SignalsRead = "SignalsRead";
    public const string AuditRead = "AuditRead";
    public const string DashboardRead = "DashboardRead";
}
